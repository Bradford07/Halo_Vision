# Team 14 Git Rules (Halo Vision)

Everyone on the team MUST follow these rules. They keep a working version
of the project available at all times, even if something breaks last minute.

## The branches

| Branch | What it is | Who changes it |
| --- | --- | --- |
| `release` | The last version we know works. **Demos always run from here.** | Only by a pull request from `main`, at milestones |
| `main` | Where finished features come together. Must always build. | Only by reviewed pull requests |
| `feature/...`, `fix/...`, `docs/...` | One task each, short-lived | You, on your own branch |

## Rules

1. **Never push directly to `main` or `release`.** All changes go through a pull request.
2. **Every pull request needs 1 approval** from a teammate before merging.
3. **Pull requests go into `main`.** Never open one from a feature branch into `release`.
4. **Before opening a pull request:** make sure the code builds and the tests pass
   (for the backend: `dotnet build` and `dotnet test` in `CompanionApp/Backend`).
5. **One task per branch.** Keep pull requests small so reviews are quick.
6. **Delete your branch after it is merged.**
7. **Never commit:** `bin/` or `obj/` folders, `*.db` files, passwords or keys,
   or real participant data. Use participant codes like "P03", never names.
8. **Do not change the Unity version.** The Headset project is locked at
   **Unity 2022.3.62f2**, the only version that runs on our HoloLens 2.

## Branch names

Use lowercase with dashes, named after the task:

- `feature/pairing` for new functionality
- `fix/profile-version-check` for bug fixes
- `docs/p4-report` for reports and READMEs

## Starting a task

    git switch main
    git pull
    git switch -c feature/your-task

## If your pull request has merge conflicts

Fix them on **your** branch, never on `main`:

    git switch feature/your-task
    git pull origin main

Resolve the conflicts in VS Code, make sure it still builds, then commit and push.
The pull request updates automatically.

## Milestones and demos (P4, P5, ...)

1. **Code freeze:** nothing gets merged into `main` in the 24 hours before a demo
   unless the whole team agrees.
2. When `main` works, open a pull request from `main` into `release` and merge it.
3. Tag that `release` commit with the assignment name and push the tag:

       git switch release
       git pull
       git tag P4_DEMO
       git push origin P4_DEMO

4. Build the submission zip from the tag:

       git archive --format=zip -o HaloVision_P4.zip P4_DEMO

5. **Demo from `release`.** If `main` breaks, `release` still works.