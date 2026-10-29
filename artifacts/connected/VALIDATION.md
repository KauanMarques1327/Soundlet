# Soundlet — Pulseira conectada

Implementação em 25/10/2026, mantendo .NET MAUI 10 e o projeto existente.

## Arquivos do aplicativo

Criados:

- `Soundlet/ConnectedPage.xaml`: tela, estado vazio, botões, histórico rolável e calendário sobreposto.
- `Soundlet/ConnectedPage.xaml.cs`: navegação, criação dos cartões, animação de 340 ms, simulação e filtro por data.
- `Soundlet/Models/AlertRecord.cs`: identificador, tipo, ocorrência, título, descrição, cor e ícone.
- `Soundlet/Models/CalendarDates.cs`: grade de 42 datas, com limites válidos de DateTime.
- `Soundlet/Services/AlertHistory.cs`: criação dos registros e persistência local em JSON, com escrita em arquivo temporário antes da substituição.
- `Soundlet/Resources/Images/connected_background.svg` e `connected_bracelet.svg`: decoração vetorial e pulseira estática.
- `Soundlet/Resources/Images/alert_fire.svg`, `alert_door.svg`, `alert_siren.svg`, `alert_bell.svg`, `alert_baby.svg` e `alert_dog.svg`: ícones dos seis tipos.

Modificados:

- `Soundlet/AppShell.xaml.cs`: registro da rota ConnectedPage.
- `Soundlet/BluetoothPage.xaml.cs`: o cartão simulado passa a navegar para ConnectedPage, com proteção contra toques repetidos. O método de voltar e as animações de busca foram preservados.

MainPage, BluetoothPage.xaml, imagens anteriores, csproj e configuração Android não foram modificados. Os novos SVGs são incluídos pela regra MauiImage já existente.

## Comportamento

- Sem dados pré-carregados. Cada toque cria um registro novo, com Guid e DateTime.Now capturado antes de aguardar persistência/animação.
- Sequência: incêndio, porta, sirene, campainha, bebê, cachorro; depois reinicia.
- Horários em HH:mm e data baseada no relógio local do Android. O emulador usado estava em GMT; seu horário difere do horário de Fortaleza. Nenhum fuso foi fixado no código.
- Mais recentes no topo. A animação expande o espaço do novo cartão, desloca os anteriores, aplica entrada vertical, opacidade e escala discreta.
- Ao simular enquanto outra data está selecionada, o filtro volta à data do novo alerta para mostrar sua chegada.
- Calendário próprio em MAUI; nenhuma dependência do seletor nativo Android. Abre no mês atual, permite navegar, selecionar dias dos meses adjacentes, filtrar e fechar por toque externo, segundo toque no botão ou Voltar do Android.
- A pulseira, seus arcos e o indicador verde não recebem animação.
- Armazenamento em `FileSystem.AppDataDirectory/alert-history.json`. Foi usado JSON em vez de adicionar SQLite ao projeto.
- A mesma operação `AlertHistory.AddAsync` pode ser reutilizada por um futuro receptor BLE. BLE real não foi implementado.

## Validação realizada

Compilação Debug para `net10.0-android`, runtime `android-x64`, com assemblies incluídos no APK. Zero erros; quatro avisos existentes em MainPage e MainActivity. Comando:

```powershell
dotnet build Soundlet/Soundlet.csproj -f net10.0-android -p:RuntimeIdentifier=android-x64 -p:EmbedAssembliesIntoApk=true -p:JavaSdkDirectory=C:/Users/kauan/AppData/Local/Android/jdk/jdk-21/jdk-21.0.12.1+1 -m:1 --no-restore -v:minimal
```

- [Log da compilação final](build-final.log).
- [150 verificações de lógica](checks.log): calendário nos anos 1, 1900, 2000, 2024, 2026 e 9999, todos os meses; alinhamento dos dias da semana; histórico vazio; sequência; IDs; persistência preservando timestamps; ordenação; filtro vazio. O programa de verificação usa os arquivos reais de modelo e serviço, substituindo apenas o diretório MAUI por um diretório temporário.
- APK instalado com ADB; instalação final em [install-final.log](install-final.log).
- [17 verificações funcionais no emulador](device-results.json): sequência, IDs, ordem visual, mês atual, próximo/anterior, filtro, retorno a hoje, toque externo, alternância do botão, Voltar fechando calendário, sétimo alerta, timestamp real, HH:mm, rolagem e retorno pelas duas telas.
- Após reinstalar e reiniciar o processo, a tela carregou novamente os sete registros salvos. Isso confirmou a persistência em execução Android.
- Uma captura durante a entrada mostrou o cartão novo parcialmente deslocado e com opacidade intermediária; a chegada também foi [gravada](arrival.mp4).
- O fundo opaco causado pelo estilo global de BoxView foi identificado por captura e corrigido com BackgroundColor transparente na camada de toque externo. A captura final confirma que a página continua visível atrás do calendário.
- Também foram verificadas larguras lógicas de aproximadamente 411 dp (densidade original 420) e 320 dp (densidade temporária 540), reiniciando o processo para aplicar a densidade. Na menor, os textos dos cartões quebram linha e o calendário usa uma área de dias rolável, com o painel inteiro dentro da área útil. O segundo toque em Hoje e o Voltar do Android para Bluetooth foram confirmados nessa largura. A densidade original foi restaurada.
- Depois das capturas, foram removidos somente os sete registros criados por esta validação. O processo foi reiniciado e a terceira tela foi aberta novamente: a árvore de acessibilidade e a captura `connected-empty.png` confirmaram o histórico vazio. O aplicativo ficou aberto nessa tela; o próximo toque em + começa pelo incêndio.

## Capturas

- [Histórico](final-history.png).
- [Estado inicial vazio](connected-empty.png).
- [Calendário sobreposto](calendar-final.png).
- [Filtro sem registros](filtered-empty.png).
- [Histórico em largura menor](narrow-history.png).
- [Calendário em largura menor](narrow-calendar.png).

## Limites da validação

- Não havia Moto G35 físico conectado. A validação foi feita no AVD MAUI_Emulator_API_36, Android API 36, x86_64.
- O emulador apresentou ANRs do System UI e demora no boot/instalação; a automação de acessibilidade precisou repetir algumas leituras que retornaram raiz nula. Os resultados aprovados estão registrados individualmente; isso não equivale a uma medição de FPS nem garante fluidez no aparelho físico.
- A decoração e os ícones foram recriados como vetores a partir das referências visuais. Não é uma reprodução pixel a pixel dos bitmaps fornecidos.
- O histórico persiste localmente, sem sincronização, exportação ou conexão BLE.

Os scripts e capturas desta pasta são artefatos de implementação/validação e não são incluídos no aplicativo.
