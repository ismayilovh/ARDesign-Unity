# 3DPatentAR Unity

3DPatentAR Unity is the mobile augmented-reality client for viewing 3D patent
models. The application scans a QR code containing a model URL, downloads the
glTF/GLB model, and lets the user inspect and manipulate it in an AR scene.

## Features

- QR-code scanning and model download progress/error handling.
- Runtime glTF/GLB loading with glTFast and Draco support.
- AR plane detection and placement using AR Foundation.
- Model translation, rotation, scaling, pivot adjustment, and screen fitting.
- Automatic and manual level-of-detail controls.
- Lighting, debug, and display settings.

## Requirements

- Unity `6000.3.17f1`.
- Git LFS for large model and source-art assets.
- The platform modules required for the intended target, such as Android with
  ARCore or iOS with ARKit.
- A supported AR-capable device for on-device testing.

## Getting started

1. Install Git LFS and clone the repository:

   ```bash
   git lfs install
   git clone https://github.com/ismayilovh/3DPatentAR-Unity.git
   ```

2. Add the cloned directory to Unity Hub and open it with Unity `6000.3.17f1`.
3. Allow Unity Package Manager to restore the dependencies in `Packages/`.
4. Open `Assets/Scenes/MyArScene.unity`.
5. Select the appropriate build profile and XR loader for the target platform.

The Android build profile is stored at
`Assets/Settings/Build Profiles/Android Build.asset`. `MyArScene` is the only
scene currently enabled in the build settings.

## Model-loading flow

```text
QR code containing a URL
          |
          v
UnityWebRequest downloads the model
          |
          v
glTFast imports the GLB/glTF scene
          |
          v
The model is prepared and placed in AR
```

The Unity client does not run the model converter directly. It expects the QR
code URL to return a model that the runtime importer can load.

## Project structure

```text
Assets/
  Editor/                 Project-specific Unity editor tooling
  Prefabs/                AR, UI, QR, and model prefabs
  Scenes/                 Application and development scenes
  Scripts/
    LOD/                   Runtime level-of-detail generation
    QR/                    QR scanning and model downloading
    UI/                    Menus, popups, settings, and diagnostics
  Settings/               Render-pipeline and build-profile assets
  XR/                     XR loaders and simulation settings
Documentation/
  Doxyfile                Doxygen configuration
  doxygen-awesome.css     Documentation theme
Packages/                 Unity Package Manager manifest and lock file
ProjectSettings/          Versioned Unity project configuration
```

`Library/`, build output, generated LOD assets, local content-package caches,
and generated documentation are intentionally excluded from version control.

## Documentation

The API documentation is generated from the C# files under `Assets/Scripts`.
To build it locally, install Doxygen and run:

```bash
cd Documentation
doxygen Doxyfile
```

Open `Documentation/output/html/index.html` after generation. The output is
ignored by Git. GitHub Actions builds the documentation for pull requests and
uploads the HTML as a workflow artifact. Builds from `main` also publish the
generated site to the separate `gh-pages` branch.

Configure GitHub Pages to deploy from the `gh-pages` branch at `/ (root)`.

## Development notes

- Keep `Packages/packages-lock.json` committed for reproducible dependencies.
- Add large supported binary formats through Git LFS.
- Do not commit generated content from `Assets/UMS_LODs` or
  `Documentation/output`.
- Unity Force Text serialization is enabled. Configure UnityYAMLMerge locally
  when resolving scene, prefab, or asset conflicts.
- Keep feature work on short-lived branches and merge it into `main` through
  pull requests.

## License

Original project code is licensed under the [MIT License](LICENSE). Unity
packages, imported samples, fonts, models, textures, and other third-party
assets remain subject to their respective licenses.
