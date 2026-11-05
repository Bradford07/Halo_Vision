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

## Large features (e.g. the segmentation model)

**First, try to split it into small pieces** that each work on their own and can be
merged into `main` separately. For example, segmentation can be split by our P3 classes:

1. `feature/sensor-capture`: camera frames (`SensorCapture`)
2. `feature/load-model`: load the model (`TrailPerception.loadModel()`)
3. `feature/segment-test-image`: run `segment()` on a saved trail photo, in a test
4. `feature/find-edges`: `findEdges()` on saved masks
5. `feature/render-path`: connect it all to `CueRenderer`

If a piece is merged but not ready to show, keep it switched off with a setting
(for example `useSegmentation = false`) until the last piece is done.

**If the work can't be split, or is an experiment that might not work out** (like trying
different models), use one umbrella branch, such as `feature/segmentation`:

- Make smaller branches from it and open pull requests **into the umbrella branch**.
  These still need 1 approval.
- **Pull `main` into the umbrella branch at least twice a week**, so it never falls far behind:

      git switch feature/segmentation
      git pull origin main

- When it works on the headset, open **one** pull request from the umbrella branch into `main`.
- If the experiment fails, delete the branch. `main` was never affected.

## Large files (models, datasets, videos)

- **Never commit a file over 50 MB.** GitHub rejects files over 100 MB, and every
  version of a big file stays in the repo history forever, which makes cloning slow.
- **Do not commit trained models (`.onnx`, Sentis files), datasets, or training videos**
  until the team agrees on where they live (Git LFS or shared storage).
- Training footage must never include anyone who has not given consent.

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