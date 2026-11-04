# ElBruno.Text2Image 1.7.1

This release adds the t2i Agent Skill and its reference guides to the
`ElBruno.Text2Image.Cli` NuGet tool package under `skills/t2i/`. Compatible
NuGet inspection tools can inspect the packaged skill directly.

The existing setup workflow is unchanged: run `t2i init` to install the skill
and references for GitHub Copilot or Claude Code. Installing the .NET tool
does not automatically activate the skill in an agent. Run `t2i upgrade` to
refresh an existing managed skill after updating the tool; unrelated skills
and user-owned reference files are preserved.

All package projects are released as version `1.7.1`.
