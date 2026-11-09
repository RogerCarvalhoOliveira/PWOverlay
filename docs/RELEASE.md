# Publicar uma versão

## 1. Preparar

Atualize `VERSION`, os nomes de download no README/manual e `CHANGELOG.md`. O build usa `VERSION` para gerar metadados dos quatro executáveis e informar a versão ao instalador. Se o formato das rotas ou do estado mudou, aumente a versão de esquema correspondente. Mudanças devem continuar aceitando o estado da versão anterior sempre que possível. Antes da 1.0, confira também `PREPARACAO-1.0.md`.

## 2. Validar e compilar

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate-data.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1 -Package
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1 -Package -Installer
powershell -ExecutionPolicy Bypass -File .\scripts\test-package.ps1 -Installer
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
- `SEM-PW-HELPER.txt`, com associação por cliente, reinício e troca de personagem;
- utilitário `PW Memory Probe.exe` no ZIP.
- auxiliar `PW Maintenance.exe`, também instalado para liberar arquivos em atualizações/desinstalações.

O instalador exige Inno Setup 6 somente no computador de desenvolvimento. O computador de destino precisa do .NET Framework 4.8 ou compatível; o instalador verifica esse requisito. Não precisa do Inno Setup.

`assets/branding/PWOverlay.ico` é incorporado aos executáveis e ao instalador. A origem e a regeneração do ícone estão documentadas em `assets/branding/README.md`; o build normal não precisa do gerador de imagens nem do Pillow.

Não inclua `state.json`, logs, nomes de personagens reais ou arquivos da instalação do jogo.

## 5. Atualizar uma instalação

Feche o executável que será substituído e faça backup do binário anterior. O estado do overlay fica em `%LOCALAPPDATA%` e não precisa ser removido. O script `install-borderless-task.ps1` deve ser executado deliberadamente; o build nunca altera a instalação do jogo.

O instalador chama o auxiliar extraído em uma pasta temporária antes do Restart Manager. O desinstalador copia o auxiliar para a pasta temporária antes de usá-lo, evitando manter o próprio arquivo instalado aberto. O auxiliar usa eventos por pasta e só considera executáveis conhecidos naquele diretório; versões antigas/sem resposta podem ser encerradas após o prazo de espera.

Para testar uma instalação separada, com todas as ferramentas e clientes reais fechados:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/test-installation.ps1 -LegacyPackagePath dist/PWQuestOverlay-Portatil-v0.11.0.zip
```

O pacote legado é opcional. O teste compila um instalador com AppId/grupo próprios, usa exclusivamente `work/installer-e2e` e remove a instalação de teste ao terminar. Não o execute em paralelo com uma sessão de jogo. A atualização da instalação principal desta máquina também foi validada na correção 0.11.1; isso não substitui testes em outros computadores.
