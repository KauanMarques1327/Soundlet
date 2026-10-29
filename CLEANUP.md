# Auditoria e limpeza conservadora

## Escopo e estado inicial

A pasta não contém `.git`, nem repositórios Git internos. `git status`, `git rev-parse` e a consulta ao índice falharam por esse motivo. Não há classificação local de arquivos rastreados/não rastreados disponível. Nenhum repositório, commit, branch ou remoto foi criado ou alterado.

Foram examinados a solução, o projeto MAUI, os includes, recursos referenciados por XAML/C#, rotas do Shell, fontes, ícone, splash, testes, scripts, configurações e a árvore de saídas geradas. Todos os recursos referenciados pelas páginas foram encontrados; imagens SVG são convertidas em PNG pelo MAUI.

O `.gitignore` existente em `Soundlet/` foi mantido. A auditoria não encontrou arquivos de assinatura ou `.env` fora das saídas geradas. Essa verificação não substitui uma revisão de segredos antes da publicação.

## Categoria A — removidos

A revisão automática bloqueou a proposta inicial de exclusão recursiva em lote; o comando não executou. A limpeza foi feita com `dotnet clean` nos projetos da aplicação e das verificações, seguida da remoção de 16 logs individuais auditados e dois arquivos Python compilados. Não foi usado `git clean`, reset, exclusão de históricos ou remoção de dados de usuário.

Antes da limpeza não havia compilação ativa. Após a limpeza, os hashes SHA-256 dos 86 arquivos protegidos permaneceram idênticos.

Total removido: **6.714 arquivos, 1.065.473.722 bytes (aproximadamente 1.016 MiB)**. Estes números descrevem a limpeza antes da compilação de validação, que recria saídas Android.

| Pasta limpa | Arquivos removidos | Método / motivo |
| --- | ---: | --- |
| `Soundlet/bin/` | 1.017 | SDK: binários, pacotes e recursos compilados |
| `Soundlet/obj/` | 5.665 | SDK: intermediários registrados pelo build |
| `artifacts/connected/checks/bin/` | 5 | SDK: saída do projeto de verificações |
| `artifacts/connected/checks/obj/` | 9 | SDK: intermediários do projeto de verificações |
| `artifacts/__pycache__/` | 1 | Bytecode recriável de `verify_identity.py` |
| `artifacts/connected/__pycache__/` | 1 | Bytecode recriável de `device_check.py` |

Logs removidos, sem referências encontradas na documentação ou nos scripts auditados:

- `bluetooth-build.log`, `bluetooth-run.log`, `bluetooth-validated-run.log`.
- `phone-build.log`, `phone-run.log`.
- `artifacts/build-lock-fix.log`, `artifacts/run-lock-fix.log`, `artifacts/video-tools.log`.
- `artifacts/icon-build-run.log`, `artifacts/icon-resize.log`.
- `artifacts/identity-build.log`, `artifacts/identity-build-server-shutdown.log`.
- `artifacts/android-run/run-before.log`.
- `artifacts/connected/device-checks.log`.
- `artifacts/wifi/run-diagnostic.log`, `artifacts/wifi/smoke.log`.

Alguns caches de restauração e bibliotecas em `obj/` e resíduos de saída em `bin/` não são removidos por `dotnet clean`. Foram mantidos e ignorados, sem forçar uma purga. Os registros completos da auditoria ficam em `artifacts/cleanup-audit*.json`, ignorados pelo Git.

## Categoria B — ignorados pelo Git

O novo `.gitignore` da raiz cobre `bin/`, `obj/`, logs de execução/build, bytecode/caches Python, caches Android/Gradle, resultados de testes, temporários, estado local de IDE, APK/AAB/APKS, logs binários MSBuild e arquivos privados de configuração/assinatura. Modelos `.env.example`, `.env.sample` e `.env.template` continuam visíveis.

Há regras específicas para os screenshots locais da raiz, `Backups/`, o perfil local `.vscode/launch.json` e a captura `artifacts/connected/persisted-device-history.json`. Esses arquivos foram preservados no disco.

Não foi ignorada a pasta `artifacts/` inteira: testes, scripts, documentação, imagens de origem e fixtures continuam visíveis. Também permanecem visíveis `Resources/`, `Platforms/`, fontes C#/XAML, projetos, solução e `.vscode/settings.json`.

As regras foram verificadas por inspeção; `git check-ignore`/status de ignorados não está disponível sem um repositório. O `.gitignore` não remove arquivos de um índice Git existente.

## Categoria C — preservados e revisão manual

- Todo o código, plataformas, recursos, configurações da aplicação e projetos de teste.
- `Backups/MainActivity.cs.20261024-133246604.bak`: backup local; nenhuma decisão sobre descarte.
- `Soundlet/Soundlet.csproj.user` e `.vscode/launch.json`: configurações locais preservadas/ignoradas; o perfil de lançamento tem caminho absoluto específico da máquina.
- `ANDROID-DEVELOPMENT.md`, `IDENTIDADE-VISUAL.md` e `artifacts/wifi/smoke.py`: contêm caminhos locais e precisam de revisão antes da publicação. Não foram editados nesta tarefa.
- Logs citados por documentação, incluindo `android-build.log`, e logs do emulador potencialmente em uso: mantidos, porém ignorados.
- Capturas, vídeos, árvores XML, resultados JSON e a captura de histórico do aparelho: podem ser evidências ou dados importantes; não foram apagados.
- `artifacts/identity/icon-source.png`: entrada legítima de um script de preparação visual; preservada.
- `Resources/AppIcon/appicon.svg` e `Resources/Images/soundlet_home.png`: recursos antigos/aparentemente não utilizados, preservados para decisão manual.
- O `MauiImage Update` para `dotnet_bot.png` aponta para um item ausente; trata-se de metadado sem `Include`, não de recurso necessário ausente. O projeto foi mantido intacto.

## Validação

- Executado `dotnet build -f net10.0-android` na raiz, incluindo restauração automática: **sucesso, zero erros**, três avisos CA1416 preexistentes em `Platforms/Android/MainActivity.cs:28`.
- O APK foi inspecionado: ícones normal/redondo/adaptativo, splash, artes de MainPage/Bluetooth/Wi-Fi e ambas as fontes continuam incluídos.
- Os 86 hashes dos arquivos protegidos foram conferidos novamente após o build, sem alterações. Nenhum código funcional, projeto, configuração de plataforma ou recurso da aplicação foi editado.
- APK instalado no emulador com atualização (`adb install -r`), sem desinstalar nem limpar dados.
- Quatro verificações básicas passaram: abertura até MainPage, primeiro toque em Wi-Fi apenas seleciona, segundo toque abre a busca existente, voltar retorna à MainPage e preserva a seleção.
- Histórico, calendário, botão `+` e todas as demais funcionalidades **não foram retestados** nesta limpeza; seus arquivos foram preservados e verificados por hash.
- A compilação recriou saídas Android em `bin/` e `obj/`, cobertas pelas regras de ignore. Isso é esperado após a validação.
- Registros locais, ignorados: `artifacts/cleanup-validation/build.log`, `integrity.json` e `smoke.json`.
- A consulta final `git status` continua informando que a pasta não é um repositório Git. Não existe índice local alterado, nem houve commit, push ou operação sobre branches.

## Preparação para publicação

Foi criado um README inicial com o objetivo, tecnologias, requisitos, comandos Android, estrutura e distinção entre funções atuais e comunicação com hardware ainda simulada/planejada.

Antes de publicar: revisar os itens da Categoria C, os caminhos locais dos scripts/documentos e a necessidade de compartilhar as evidências de desenvolvimento; escolher uma licença; inicializar o Git e conferir a seleção de arquivos e as regras de ignore antes do primeiro commit. Não há histórico local para remover arquivos de um índice, e nenhum comando de commit/push foi executado.
