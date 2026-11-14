#!/usr/bin/env python3
"""Verify that a NuGet release is indexed, has expected target assets, and its CLI runs."""

from __future__ import annotations

import io
import os
import re
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.request
import zipfile
from collections.abc import Callable, Sequence

PACKAGE_IDS = (
    "ElBruno.Text2Image",
    "ElBruno.Text2Image.Cli",
    "ElBruno.Text2Image.Cpu",
    "ElBruno.Text2Image.Cuda",
    "ElBruno.Text2Image.DirectML",
    "ElBruno.Text2Image.Foundry",
    "ElBruno.Text2Image.BlazorComponents",
)
TARGET_FRAMEWORKS = ("net8.0", "net10.0", "net11.0")
FLAT_CONTAINER_URL = "https://api.nuget.org/v3-flatcontainer"
INDEXING_ERROR = re.compile(
    r"not found in NuGet feeds|NU1101|NU1102|unable to find package",
    re.IGNORECASE,
)


class VerificationError(RuntimeError):
    """Raised when a published package cannot be verified."""


def fetch_package(
    package_id: str, version: str, base_url: str, timeout: int
) -> bytes | None:
    package = package_id.lower()
    normalized_version = version.lower()
    url = (
        f"{base_url.rstrip('/')}/{package}/{normalized_version}/"
        f"{package}.{normalized_version}.nupkg"
    )
    request = urllib.request.Request(url, headers={"User-Agent": "NuGetReleaseVerifier"})
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            return response.read()
    except urllib.error.HTTPError as error:
        if error.code == 404 or error.code == 429 or error.code >= 500:
            return None
        raise VerificationError(
            f"NuGet returned HTTP {error.code} while checking {package_id} {version}."
        ) from error
    except (TimeoutError, urllib.error.URLError):
        return None


def wait_for_packages(
    version: str,
    *,
    package_ids: Sequence[str] = PACKAGE_IDS,
    fetch: Callable[[str, str], bytes | None],
    attempts: int,
    delay_seconds: int,
    sleep: Callable[[float], None] = time.sleep,
    log: Callable[[str], None] = print,
) -> dict[str, bytes]:
    remaining = set(package_ids)
    packages: dict[str, bytes] = {}
    for attempt in range(1, attempts + 1):
        for package_id in tuple(remaining):
            content = fetch(package_id, version)
            if content:
                packages[package_id] = content
                remaining.remove(package_id)
                log(f"NuGet package available: {package_id} {version}")
        if not remaining:
            return packages
        waiting_on = ", ".join(sorted(remaining))
        log(
            f"NuGet indexing pending for {waiting_on} "
            f"(attempt {attempt}/{attempts})."
        )
        if attempt < attempts:
            sleep(delay_seconds)
    raise VerificationError(
        f"NuGet did not make these packages available after {attempts} attempts: "
        f"{', '.join(sorted(remaining))}."
    )


def validate_package_assets(package_id: str, content: bytes) -> None:
    try:
        with zipfile.ZipFile(io.BytesIO(content)) as package:
            names = package.namelist()
    except (OSError, zipfile.BadZipFile) as error:
        raise VerificationError(f"{package_id} is not a valid NuGet package.") from error

    if package_id == "ElBruno.Text2Image.Cli":
        expected_assets = tuple(
            f"tools/{framework}/any/DotnetToolSettings.xml"
            for framework in TARGET_FRAMEWORKS
        )
    else:
        expected_assets = tuple(
            f"lib/{framework}/" for framework in TARGET_FRAMEWORKS
        )

    missing = [
        path
        for path in expected_assets
        if not any(name.startswith(path) for name in names)
    ]
    if missing:
        raise VerificationError(
            f"{package_id} is missing expected target assets: {', '.join(missing)}."
        )
    print(f"Verified target assets for {package_id}: {', '.join(TARGET_FRAMEWORKS)}")


def is_indexing_error(output: str) -> bool:
    return bool(INDEXING_ERROR.search(output))


def verify_cli_install(
    version: str,
    *,
    attempts: int,
    delay_seconds: int,
    runner: Callable[..., subprocess.CompletedProcess[str]] = subprocess.run,
    sleep: Callable[[float], None] = time.sleep,
    log: Callable[[str], None] = print,
) -> None:
    with tempfile.TemporaryDirectory(prefix="t2i-nuget-verify-") as temp_dir:
        manifest_dir = os.path.join(temp_dir, "manifest")
        os.makedirs(manifest_dir)
        env = os.environ.copy()
        env["NUGET_PACKAGES"] = os.path.join(temp_dir, "packages")
        env["NUGET_HTTP_CACHE_PATH"] = os.path.join(temp_dir, "http-cache")
        manifest_result = runner(
            ["dotnet", "new", "tool-manifest", "--force"],
            cwd=manifest_dir,
            env=env,
            capture_output=True,
            text=True,
        )
        if manifest_result.returncode != 0:
            output = f"{manifest_result.stdout}\n{manifest_result.stderr}".strip()
            raise VerificationError(f"Creating a tool manifest failed:\n{output}")
        install_command = [
            "dotnet",
            "tool",
            "install",
            "ElBruno.Text2Image.Cli",
            "--version",
            version,
            "--source",
            "https://api.nuget.org/v3/index.json",
            "--ignore-failed-sources",
            "--verbosity",
            "minimal",
        ]
        for attempt in range(1, attempts + 1):
            result = runner(
                install_command,
                cwd=manifest_dir,
                env=env,
                capture_output=True,
                text=True,
            )
            output = f"{result.stdout}\n{result.stderr}".strip()
            if result.returncode == 0:
                break
            if not is_indexing_error(output):
                raise VerificationError(
                    f"Installing CLI {version} failed for a non-indexing reason:\n{output}"
                )
            log(
                f"NuGet tool index is still catching up for CLI {version} "
                f"(attempt {attempt}/{attempts})."
            )
            if attempt < attempts:
                sleep(delay_seconds)
        else:
            raise VerificationError(
                f"CLI {version} remained undiscoverable after {attempts} attempts. "
                f"Last install output:\n{output}"
            )

        result = runner(
            ["dotnet", "tool", "run", "t2i", "--", "--help"],
            cwd=manifest_dir,
            env=env,
            capture_output=True,
            text=True,
        )
        if result.returncode != 0:
            output = f"{result.stdout}\n{result.stderr}".strip()
            raise VerificationError(f"Running `t2i --help` failed:\n{output}")
        log(f"Installed CLI {version} from NuGet and verified `t2i --help`.")


def positive_int(name: str, default: int) -> int:
    value = os.environ.get(name, str(default))
    try:
        parsed = int(value)
    except ValueError as error:
        raise VerificationError(f"{name} must be a positive integer.") from error
    if parsed < 1:
        raise VerificationError(f"{name} must be a positive integer.")
    return parsed


def main(argv: Sequence[str]) -> int:
    if len(argv) != 2 or not re.fullmatch(r"\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?", argv[1]):
        print(f"Usage: {argv[0]} <package-version>", file=sys.stderr)
        return 2
    version = argv[1]
    try:
        attempts = positive_int("NUGET_VERIFY_ATTEMPTS", 60)
        delay_seconds = positive_int("NUGET_VERIFY_DELAY_SECONDS", 20)
        timeout = positive_int("NUGET_VERIFY_TIMEOUT_SECONDS", 30)
        base_url = os.environ.get("NUGET_FLAT_CONTAINER_URL", FLAT_CONTAINER_URL)
        packages = wait_for_packages(
            version,
            fetch=lambda package_id, package_version: fetch_package(
                package_id, package_version, base_url, timeout
            ),
            attempts=attempts,
            delay_seconds=delay_seconds,
        )
        for package_id, content in packages.items():
            validate_package_assets(package_id, content)
        verify_cli_install(
            version, attempts=attempts, delay_seconds=delay_seconds
        )
        return 0
    except VerificationError as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
