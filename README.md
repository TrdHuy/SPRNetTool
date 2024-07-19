<div align="center">
  <a href="https://github.com/othneildrew/Best-README-Template">
    <img src="SPRNetTool/Resources/logo_500.png" alt="Logo" width="200" height="200 ">
  </a>

  <h1 align="center">ArtWiz</h1>
</div>

### For dev on Window run with cmd

#### 👉 Clone with hook
``` cmd
git clone "https://github.com/TrdHuy/ArtWiz.git" && cd "ArtWiz" && curl -s https://raw.githubusercontent.com/TrdHuy/ArtWiz/document_v1.0/commit-msg > .git\hooks\commit-msg && curl -s https://raw.githubusercontent.com/TrdHuy/ArtWiz/document_v1.0/pre-commit > .git\hooks\pre-commit
```

#### 👉 Hook
``` cmd
curl -s https://raw.githubusercontent.com/TrdHuy/ArtWiz/document_v1.0/commit-msg > .git\hooks\commit-msg && curl -s https://raw.githubusercontent.com/TrdHuy/ArtWiz/document_v1.0/pre-commit > .git\hooks\pre-commit
```

#### 👉 To create a new version up automatically
``` cmd
gh workflow run AutoVersionBump -F vT="minor" -F force="true"
```

```
In there:
vT is Version up type
vT="major"
vT="minor"
vT="patch"
vT="build"

force indicates that, you will version up even if previous commit is version up commit
force="true"
force="false"
```

#### 👉 Note: new versions will be released automatically every Friday at 18:00 (UTC+7), [detail](https://github.com/TrdHuy/ArtWiz/blob/dev/.github/workflows/dot-net-auto-version-up.yml)

#### 👉 Internal nuget source address
```
https://nuget.pkg.github.com/TrdHuy/index.json
```
###### 💥 For nuget source password (PAT) please contact: trdtranduchuy@gmail.com or visit [Trd workflow guidelines](https://github.com/BalalaX/TrdRepoNote?tab=readme-ov-file#pat)
