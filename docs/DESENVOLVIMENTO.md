# Tutorial de desenvolvimento

## 1. Como o protótipo foi criado

O desenvolvimento começou com quatro requisitos:

1. funcionar sobre o PW em modo janela sem bordas;
2. acompanhar vários `elementclient`;
3. coexistir com o PW Helper;
4. não acessar memória, pacotes ou arquivos internos do jogo.

O monitor borderless já identificava o cliente ativo por `HWND`. O overlay reutilizou o mesmo princípio e passou a ler o título público que o PW Helper atribui à janela.

O título real observado foi:

```text
Cauton[EP]
```

Depois, um segundo cliente mostrou:

```text
Cauton[WB]
```

Isso revelou que o nome sozinho não era uma identidade suficiente. A chave persistente foi alterada para `PERSONAGEM|CLASSE`.

O painel `TODOS` foi o primeiro identificado como uma janela `Chrome_WidgetWin_1`, `TOPMOST` e capaz de receber foco. A detecção foi generalizada: qualquer janela visível do processo `PW Helper`, exceto a janela principal intitulada exatamente `PW Helper`, é tratada como painel flutuante e reelevada depois do próprio tracker.

## 2. Ambiente de compilação

O código usa WinForms e .NET Framework para produzir um executável pequeno e portátil.

O script procura o compilador nestes locais:

```text
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
```

Não é necessário instalar o Visual Studio quando um desses compiladores está disponível.

## 3. Compilar

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

Para gerar também o ZIP:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1 -Package
```

## 4. Executar em desenvolvimento

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-dev.ps1
```

O script encerra apenas uma instância anterior de `PW Quest Overlay` e inicia o executável recém-compilado.

## 5. Alterar a interface

Edite `src/OverlayForm.cs`.

Áreas principais:

- construtor: tamanho, cores, textos e controles;
- `MonitorTick`: foco, visibilidade e ordem Z;
- `RefreshMissionDisplay`: conteúdo mostrado;
- `CompleteCurrent`, `SkipCurrent`, `GoBack`: progresso;
- `FindDefaultPosition`: posição inicial e prevenção de colisão;
- `AttachDragHandlers` / `FinishDrag`: arraste com captura no controle que recebeu o clique; soltura do esquerdo e perda de captura salvam a posição;
- `RegisterHotkeys`: atalhos globais.

O código deve continuar compatível com o compilador do .NET Framework. Evite recursos que exijam SDK moderno sem atualizar também o processo de build.

## 6. Alterar detecção de título

Os padrões ficam em `ClientIdentity.cs`, com wrappers compatíveis em `OverlayForm.cs`:

- `Primary`: formato `Nome[CLASSE]`;
- `Token`: formato alternativo com sigla delimitada.

Não procure a sigla como substring livre. Um nome como `Wrex` não deve ser identificado como `WR`.

Sempre mantenha o fallback manual.

As dez siglas devem permanecer alinhadas nos dois padrões, no menu rápido, em `IsValidClass`, na tabela de Perfis e em `RouteRepository.GetForClass`. O teste `scripts/test-overlay-interactions.ps1` verifica os padrões e a aceitação manual para todas elas, além dos eventos reais dos controles de arraste com estado isolado. O roteiro dos novos guias está em `GUIAS-DEZ-CLASSES.md`.

`OpenProfileManager` associa uma sessão de janela a nome/classe e permite usar um perfil salvo. `ClientBindings` não é serializado; nunca persista um PID para reaplicar automaticamente. `OpenTitleAssociations` mantém as antigas associações em `AppState.ClassAssociations`. Confira `docs/SEM-PW-HELPER.md` para os casos de multiclient e reinício.

## 7. Alterar comportamento do PW Helper

O método `RaiseOverlayStack` eleva:

1. o tracker;
2. todas as janelas flutuantes visíveis do processo `PW Helper`, independentemente do título.

Não use `SetForegroundWindow` nesse fluxo. Isso transferiria teclado e mouse para o Helper durante o jogo.

## 8. Adicionar uma preferência

1. Adicione a propriedade em `AppState`, em `Models.cs`.
2. Defina um valor padrão no construtor de `AppState`.
3. Carregue com fallback em `StateStore.Load`.
4. Adicione o controle no menu ou painel.
5. Chame `_store.Save(_state)` após a alteração.
6. Teste com um `state.json` antigo e um novo.

O campo `ClassAssociations`, por exemplo, foi adicionado dessa forma. Como o JSON antigo não contém essa propriedade, `StateStore.Load` cria um dicionário vazio sem perder o progresso já salvo.

## 9. Diagnóstico

Use:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\inspect-state.ps1
Get-Content "$env:LOCALAPPDATA\PWQuestOverlay\overlay.log" -Tail 100
```

O log deve registrar somente informações necessárias para diagnóstico, como perfil, troca de cliente e missão alterada.

## 10. Antes de commitar

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate-data.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\test.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1 -Package
git diff --check
git status --short
```

Depois teste conforme `docs/TESTES.md`.
