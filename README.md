
<div align="center">
  <a href="https://github.com/TrdHuy/ArtWiz">
    <img src="SPRNetTool/Resources/logo_500.png" alt="Logo" width="200" height="200">
  </a>

  <h1 align="center">ArtWiz</h1>
  <p align="center">
    A powerful tool for managing and editing game assets, built with simplicity and efficiency in mind.
  </p>
</div>

---

## Quick Start for Development on Windows

### 🚀 Clone with Git Hooks
Clone the repository and automatically set up Git hooks:
```cmd
git clone "https://github.com/TrdHuy/ArtWiz.git" && cd "ArtWiz" && curl -s https://raw.githubusercontent.com/TrdHuy/ArtWiz/document_v1.0/commit-msg > .git\hooks\commit-msg && curl -s https://raw.githubusercontent.com/TrdHuy/ArtWiz/document_v1.0/pre-commit > .git\hooks\pre-commit
```

### 🔧 Set Up Git Hooks Separately
If you need to manually set up the hooks, use the following commands:
```cmd
curl -s https://raw.githubusercontent.com/TrdHuy/ArtWiz/document_v1.0/commit-msg > .git\hooks\commit-msg
curl -s https://raw.githubusercontent.com/TrdHuy/ArtWiz/document_v1.0/pre-commit > .git\hooks\pre-commit
```

---

## 📦 Automatic Version Management

### Create a New Version
Run the following command to bump the project version:
```cmd
gh workflow run AutoVersionBump -F branch="minor"
```

#### Version Parameters:
- **Version Types (vT)**:
  - `major`: Major changes or breaking updates.
  - `minor`: New features or enhancements.
  - `patch`: Bug fixes or small updates.
  - `build`: Workflow-related changes only.
  
- **Force Update**:
  - `force="true"`: Force version bump even if the last commit is a version commit.
  - `force="false"`: Do not force version bump.

#### Versioning Rules:
- Use **`build`** for workflow-only changes.
- Use **`patch`** for any product-related changes.

### Automatic Release
New versions are automatically released every **Friday at 18:00 (UTC+7)**. For more details, see the [Versioning Workflow](https://github.com/TrdHuy/ArtWiz/blob/dev/.github/workflows/dot-net-auto-version-up.yml).

---

## 🛠 Internal NuGet Source

### NuGet Source Address:
```
https://nuget.pkg.github.com/TrdHuy/index.json
```

To access the NuGet source:
- Request the NuGet source password (PAT) by contacting: [trdtranduchuy@gmail.com](mailto:trdtranduchuy@gmail.com).
- Alternatively, refer to the [Trd Workflow Guidelines](https://github.com/BalalaX/TrdRepoNote?tab=readme-ov-file#pat).

---

## 📖 Additional Resources
- **[Build Permission Guide](./wiki/BuildRequestPermission.md):** Learn how to request build permissions for the project.

---

<div align="center">
  <strong>Happy Coding! 🚀</strong>
</div>
