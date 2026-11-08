# Publicar uma versão

## 1. Preparar

Atualize `CHANGELOG.md` e, se o formato das rotas ou do estado mudou, aumente a versão de esquema correspondente. Mudanças devem continuar aceitando o estado da versão anterior sempre que possível.

## 2. Validar e compilar

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate-data.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1 -Package
git diff --check
```

Execute o roteiro de `docs\TESTES.md`, principalmente o teste multiclient e a associação manual.

## 3. Commit e tag

```powershell
git add .
git commit -m "Release vX.Y.Z"
git tag -a vX.Y.Z -m "PW Quest Overlay vX.Y.Z"
git push origin main
git push origin vX.Y.Z
```

## 4. Artefatos

O pacote fica em `dist\PW Quest Overlay Prototipo.zip`. Ele contém:

- `PW Quest Overlay.exe`;
- `PW Borderless.exe`;
- `quest_routes.json`;
- `LEIA-ME.txt`.

Não inclua `state.json`, logs, nomes de personagens reais ou arquivos da instalação do jogo.

## 5. Atualizar uma instalação

Feche o executável que será substituído e faça backup do binário anterior. O estado do overlay fica em `%LOCALAPPDATA%` e não precisa ser removido. O script `install-borderless-task.ps1` deve ser executado deliberadamente; o build nunca altera a instalação do jogo.
