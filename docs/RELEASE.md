# Publicar uma versão

## 1. Preparar

Atualize `CHANGELOG.md` e, se o formato das rotas ou do estado mudou, aumente a versão de esquema correspondente. Mudanças devem continuar aceitando o estado da versão anterior sempre que possível.

## 2. Validar e compilar

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate-data.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1 -Package
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1 -Package -Installer
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

Os pacotes ficam em `dist\PWQuestOverlay-Portatil-vX.Y.Z.zip` e `dist\PWQuestOverlay-Setup-vX.Y.Z.exe`. A versão portátil contém:

- `PW Quest Overlay.exe`;
- `PW Borderless.exe`;
- `PW Overlay Central.exe`;
- `quest_routes.json`;
- `memory_offsets.json` e `task_catalog_144.tsv`;
- `rota_leveling_pw_144.xlsx`, com cinco abas;
- `LEIA-ME.txt`;
- `INSTALACAO.txt`, `GUIAS-DEZ-CLASSES.txt` e `GUIA-61-105.txt`;
- utilitário `PW Memory Probe.exe` no ZIP.

O instalador exige Inno Setup 6 somente no computador de desenvolvimento. O computador de destino precisa apenas do .NET Framework 4.x; não precisa do Inno Setup.

Não inclua `state.json`, logs, nomes de personagens reais ou arquivos da instalação do jogo.

## 5. Atualizar uma instalação

Feche o executável que será substituído e faça backup do binário anterior. O estado do overlay fica em `%LOCALAPPDATA%` e não precisa ser removido. O script `install-borderless-task.ps1` deve ser executado deliberadamente; o build nunca altera a instalação do jogo.
