#!/usr/bin/env python3
# SPDX-License-Identifier: AGPL-3.0-or-later

import argparse
import os
import subprocess
from pathlib import Path
from typing import Iterable

import requests

PUBLISH_TOKEN = os.environ["PUBLISH_TOKEN"]
VERSION = os.environ["GITHUB_SHA"]
FORK_ID = os.environ["FORK_ID"]

RELEASE_DIR = Path("release")

ROBUST_CDN_URL = os.environ.get("ROBUST_CDN_URL", "https://cdn.station14.ru/")
if not ROBUST_CDN_URL.endswith("/"):
    ROBUST_CDN_URL += "/"


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--fork-id", default=FORK_ID)
    parser.add_argument("--version", default=VERSION)
    parser.add_argument("--cdn-url", default=ROBUST_CDN_URL)

    args = parser.parse_args()

    fork_id = args.fork_id
    version = args.version
    cdn_url = args.cdn_url

    if not cdn_url.endswith("/"):
        cdn_url += "/"

    if not RELEASE_DIR.exists():
        raise RuntimeError(f"Release directory does not exist: {RELEASE_DIR}")

    files = list(get_files_to_publish())
    if not files:
        raise RuntimeError(f"No files found in release directory: {RELEASE_DIR}")

    session = requests.Session()
    session.headers.update({
        "Authorization": f"Bearer {PUBLISH_TOKEN}",
    })

    print(f"Starting publish on Robust.Cdn")
    print(f"CDN URL: {cdn_url}")
    print(f"Fork ID: {fork_id}")
    print(f"Version: {version}")
    print(f"Files: {len(files)}")

    data = {
        "version": version,
        "engineVersion": get_engine_version(),
    }

    resp = session.post(
        f"{cdn_url}fork/{fork_id}/publish/start",
        json=data,
        headers={"Content-Type": "application/json"},
        timeout=60,
    )
    resp.raise_for_status()

    print("Publish successfully started, adding files...")

    for file in files:
        print(f"Publishing {file}")

        with file.open("rb") as f:
            resp = session.post(
                f"{cdn_url}fork/{fork_id}/publish/file",
                data=f,
                headers={
                    "Content-Type": "application/octet-stream",
                    "Robust-Cdn-Publish-File": file.name,
                    "Robust-Cdn-Publish-Version": version,
                },
                timeout=600,
            )

        resp.raise_for_status()

    print("Successfully pushed files, finishing publish...")

    resp = session.post(
        f"{cdn_url}fork/{fork_id}/publish/finish",
        json={"version": version},
        headers={"Content-Type": "application/json"},
        timeout=60,
    )
    resp.raise_for_status()

    print("SUCCESS!")


def get_files_to_publish() -> Iterable[Path]:
    for file in RELEASE_DIR.iterdir():
        if file.is_file():
            yield file


def get_engine_version() -> str:
    proc = subprocess.run(
        ["git", "describe", "--tags", "--abbrev=0"],
        stdout=subprocess.PIPE,
        cwd="RobustToolbox",
        check=True,
        encoding="utf-8",
    )

    tag = proc.stdout.strip()

    if not tag.startswith("v"):
        raise RuntimeError(f"Engine tag does not start with 'v': {tag}")

    return tag[1:]


if __name__ == "__main__":
    main()
