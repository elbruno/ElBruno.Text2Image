import importlib.util
import subprocess
import unittest
from pathlib import Path


SCRIPT = (
    Path(__file__).resolve().parents[2]
    / "scripts"
    / "verify_nuget_release.py"
)
SPEC = importlib.util.spec_from_file_location("verify_nuget_release", SCRIPT)
verifier = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(verifier)


class VerifyNuGetReleaseTests(unittest.TestCase):
    def test_waits_for_indexing_then_returns_available_packages(self):
        responses = {"Core": [None, b"package"], "CLI": [b"package"]}
        sleeps = []
        result = verifier.wait_for_packages(
            "1.2.3",
            package_ids=("Core", "CLI"),
            fetch=lambda package_id, _version: responses[package_id].pop(0),
            attempts=3,
            delay_seconds=5,
            sleep=sleeps.append,
            log=lambda _message: None,
        )
        self.assertEqual(result, {"Core": b"package", "CLI": b"package"})
        self.assertEqual(sleeps, [5])

    def test_persistent_indexing_delay_fails_after_bounded_attempts(self):
        sleeps = []
        with self.assertRaisesRegex(verifier.VerificationError, "after 2 attempts"):
            verifier.wait_for_packages(
                "1.2.3",
                package_ids=("Core",),
                fetch=lambda _package_id, _version: None,
                attempts=2,
                delay_seconds=3,
                sleep=sleeps.append,
                log=lambda _message: None,
            )
        self.assertEqual(sleeps, [3])

    def test_validates_all_target_assets_and_rejects_missing_assets(self):
        import io
        import zipfile

        package_file = io.BytesIO()
        with zipfile.ZipFile(package_file, "w") as archive:
            for framework in verifier.TARGET_FRAMEWORKS:
                archive.writestr(f"lib/{framework}/Example.dll", b"")
        verifier.validate_package_assets("ElBruno.Text2Image", package_file.getvalue())

        with self.assertRaisesRegex(verifier.VerificationError, "net11.0"):
            verifier.validate_package_assets(
                "ElBruno.Text2Image", self.package_without_net11()
            )

    def test_cli_install_retries_indexing_error_and_runs_help(self):
        calls = []

        def runner(command, **_kwargs):
            calls.append(command)
            if command[:3] == ["dotnet", "tool", "install"] and sum(
                call[:3] == ["dotnet", "tool", "install"] for call in calls
            ) == 1:
                return subprocess.CompletedProcess(
                    command, 1, "", "error NU1101: Unable to find package"
                )
            return subprocess.CompletedProcess(command, 0, "OK", "")

        sleeps = []
        verifier.verify_cli_install(
            "1.2.3",
            attempts=2,
            delay_seconds=4,
            runner=runner,
            sleep=sleeps.append,
            log=lambda _message: None,
        )
        self.assertEqual(sleeps, [4])
        self.assertEqual(calls[-1], ["dotnet", "tool", "run", "t2i", "--", "--help"])

    def test_cli_install_surfaces_non_indexing_failure_without_retry(self):
        calls = []

        def runner(command, **_kwargs):
            calls.append(command)
            if command[:4] == ["dotnet", "new", "tool-manifest", "--force"]:
                return subprocess.CompletedProcess(command, 0, "", "")
            return subprocess.CompletedProcess(command, 1, "", "authentication failed")

        with self.assertRaisesRegex(verifier.VerificationError, "non-indexing reason"):
            verifier.verify_cli_install(
                "1.2.3",
                attempts=3,
                delay_seconds=4,
                runner=runner,
                sleep=lambda _delay: self.fail("unexpected retry"),
                log=lambda _message: None,
            )
        self.assertEqual(
            sum(call[:3] == ["dotnet", "tool", "install"] for call in calls), 1
        )

    @staticmethod
    def package_without_net11():
        import io
        import zipfile

        package_file = io.BytesIO()
        with zipfile.ZipFile(package_file, "w") as archive:
            archive.writestr("lib/net8.0/Example.dll", b"")
            archive.writestr("lib/net10.0/Example.dll", b"")
        return package_file.getvalue()


if __name__ == "__main__":
    unittest.main()
