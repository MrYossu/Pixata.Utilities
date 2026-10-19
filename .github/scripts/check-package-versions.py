"""
Checks that every package the release workflow publishes has a version that isn't on nuget.org yet, if its code has changed since
that version was published.

The publish step skips a package whose version is already on nuget.org, without failing. So if a package's code changes and its
<Version> isn't bumped, the release silently leaves the old package in place, and anything packed against the new code (eg
Pixata.Blazor, which depends on Pixata.Extensions) points at a version that doesn't have it. See issue #91.

For each project, this asks nuget.org whether its current version is there. If it is, it reads the commit that version was built
from (the nuspec's <repository commit="...">, which the SDK adds when packing) and lists the files in the project's folder that
have changed since. Markdown files are ignored, as a documentation change alone doesn't need a new package.

By default, the projects are the PROJECT_FILE_PATH entries in .github/workflows/release.yml, so a new package is checked as soon
as it's published. Pass project files as arguments to check others.

Exits with 1 if any package needs a new version, unless --warn-only is passed, in which case the problems are reported as
warnings. Run from the root of the repository, with enough history to include the published commits (fetch-depth: 0).
"""

import json
import os
import re
import subprocess
import sys
import urllib.error
import urllib.request
import xml.etree.ElementTree as ElementTree

NUGET = "https://api.nuget.org/v3-flatcontainer"
RELEASE_WORKFLOW = ".github/workflows/release.yml"


def projects_from_release_workflow():
  with open(RELEASE_WORKFLOW, encoding="utf-8") as workflow:
    return re.findall(r"PROJECT_FILE_PATH:\s*(\S+)", workflow.read())


def read_project(path):
  with open(path, encoding="utf-8-sig") as project:
    text = project.read()
  version = re.search(r"<Version>\s*([^<\s]+)\s*</Version>", text)
  package_id = re.search(r"<PackageId>\s*([^<\s]+)\s*</PackageId>", text)
  return (package_id.group(1) if package_id else os.path.splitext(os.path.basename(path))[0]), (version.group(1) if version else None)


def get(url):
  try:
    with urllib.request.urlopen(url, timeout=30) as response:
      return response.read()
  except urllib.error.HTTPError as e:
    if e.code == 404:
      return None
    raise


def published_versions(package_id):
  index = get(f"{NUGET}/{package_id.lower()}/index.json")
  return [] if index is None else [v.lower() for v in json.loads(index)["versions"]]


def published_commit(package_id, version):
  nuspec = get(f"{NUGET}/{package_id.lower()}/{version.lower()}/{package_id.lower()}.nuspec")
  if nuspec is None:
    return None
  repository = next((e for e in ElementTree.fromstring(nuspec).iter() if e.tag.endswith("}repository") or e.tag == "repository"), None)
  return None if repository is None else repository.get("commit")


def git(*args):
  return subprocess.run(["git", *args], capture_output=True, text=True)


def changed_files(commit, folder):
  diff = git("diff", "--name-only", commit, "HEAD", "--", folder)
  if diff.returncode != 0:
    return None
  return [f for f in diff.stdout.splitlines() if f and not f.lower().endswith(".md")]


def report(level, message):
  # Shown as an annotation on the run when this is run by GitHub Actions, and as plain text otherwise
  prefix = f"::{level}::" if os.environ.get("GITHUB_ACTIONS") == "true" else f"{level.upper()}: "
  print(prefix + message.replace("\n", "%0A" if prefix.startswith("::") else "\n"))


def main():
  args = [a for a in sys.argv[1:] if not a.startswith("--")]
  warn_only = "--warn-only" in sys.argv
  problems = 0
  for project in args or projects_from_release_workflow():
    package_id, version = read_project(project)
    if version is None:
      report("warning", f"{project} has no <Version>, so it wasn't checked")
      continue
    if version.lower() not in published_versions(package_id):
      print(f"{package_id} {version} isn't on nuget.org yet, so it will be published")
      continue
    commit = published_commit(package_id, version)
    if commit is None or git("cat-file", "-e", f"{commit}^{{commit}}").returncode != 0:
      report("warning", f"{package_id} {version} is on nuget.org, but the commit it was built from isn't known here, so it wasn't checked")
      continue
    changed = changed_files(commit, os.path.dirname(project) or ".")
    if changed is None:
      report("warning", f"Couldn't compare {package_id} with commit {commit[:7]}, so it wasn't checked")
    elif changed:
      problems += 1
      files = "\n".join(f"  {f}" for f in changed)
      report("warning" if warn_only else "error",
             f"{package_id} {version} is already on nuget.org (built from {commit[:7]}), but its code has changed since, so the release would "
             f"skip it. Bump <Version>, <AssemblyVersion> and <FileVersion> in {project}. Changed files...\n{files}")
    else:
      print(f"{package_id} {version} is on nuget.org, and hasn't changed since")
  return 1 if problems and not warn_only else 0


if __name__ == "__main__":
  sys.exit(main())
