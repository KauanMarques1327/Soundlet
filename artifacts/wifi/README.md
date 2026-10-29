# Implementação Wi-Fi do Soundlet

Concluída e validada em 26/10/2026 no emulador Android API 36 (emulator-5554, 1080 × 2400).

## Comportamento

Bluetooth inicia selecionado. Tocar em uma opção diferente muda apenas a seleção; tocar na opção selecionada abre a busca correspondente, sem prazo entre toques. A seleção permanece ao voltar à MainPage.

Os controles reais de toque compartilham o viewport AspectFill com o desenho nativo dos botões. O fundo inicial novo contém somente a arte sem os botões antigos. A atualização visual usa Invalidate, sem trocar a imagem de fundo.

WifiPage reaproveita LoadingArcDrawable e o ciclo de animação Bluetooth (16 ms, 1400 ms, Easing.Linear), com coordenadas próprias. As animações são interrompidas em OnDisappearing/Window.Stopped e retomadas em OnAppearing/Window.Resumed.

Ambas as buscas navegam à ConnectedPage existente. O fluxo Wi-Fi informa connection=wifi para atualizar o rótulo e a mensagem de demonstração. O retorno usa GoToAsync("..") e a pilha do Shell. Histórico, calendário e ingestão de alertas continuam compartilhados.

## Arquivos da aplicação

Criados:

- ../../Soundlet/WifiPage.xaml
- ../../Soundlet/WifiPage.xaml.cs
- ../../Soundlet/ConnectionOptionsDrawable.cs
- ../../Soundlet/Resources/Images/soundlet_home_background.png
- ../../Soundlet/Resources/Images/soundlet_wifi.png

Modificados:

- ../../Soundlet/MainPage.xaml
- ../../Soundlet/MainPage.xaml.cs
- ../../Soundlet/AppShell.xaml.cs
- ../../Soundlet/ConnectedPage.xaml
- ../../Soundlet/ConnectedPage.xaml.cs

BluetoothPage.xaml/.cs, LoadingArcDrawable.cs, serviços/modelos de histórico, calendário, ícone, splash, imagens originais e Soundlet.csproj foram preservados.

Os recursos visuais foram preparados com a ferramenta imagegen integrada. [Prompts completos, origem e caminhos dos recursos](visual-assets.md).

## Compilação e instalação

Comando inicial: dotnet build Soundlet/Soundlet.csproj -f net10.0-android -c Debug --no-restore.

Comando da versão final: dotnet build Soundlet/Soundlet.csproj -f net10.0-android -c Debug -t:Install --no-restore "-p:AdbTarget=-s emulator-5554".

Resultado final: sucesso, zero erros. Permanecem três avisos CA1416 preexistentes em Platforms/Android/MainActivity.cs:28. Nenhum framework, workload ou pacote foi alterado.

[Log da compilação e instalação finais](final-build-install.log).

## Testes

35 verificações de interação passaram. [Resultados estruturados](results.json), [log final](smoke-final.log), [roteiro ADB](smoke.py).

| Cenário solicitado | Resultado / evidência |
| --- | --- |
| 1. Bluetooth selecionado ao abrir | Passou — 01-home-initial.png |
| 2. Primeiro toque Wi-Fi apenas seleciona | Passou — 02-wifi-selected.png |
| 3. Segundo toque Wi-Fi abre busca | Passou — 03-wifi-search.xml; há espera entre toques |
| 4. Fidelidade visual à referência | Inspeção visual de 03-wifi-search-a.png: curvas, fundo, voltar, símbolo, título, frase e cartão |
| 5. Arco central Wi-Fi animado | Passou — comparação de duas capturas, símbolo central estático |
| 6. Indicador pequeno e alinhamento | Passou — movimento detectado e inspeção visual sem cobrir letras |
| 7. Regiões do cartão clicáveis | Passou — ícone, nome, subtítulo, espaço vazio, seta e proximidades das bordas esquerda/direita |
| 8. Terceira tela existente | Passou — 04-wifi-connected.png e mesma rota ConnectedPage |
| 9. Retorno à busca Wi-Fi | Passou nos sete acessos pelo cartão |
| 10. Retorno à MainPage e troca para Bluetooth | Passou; seleção Wi-Fi preservada antes da troca |
| 11. Bluetooth só abre quando já selecionado | Passou — 10-bluetooth-selected.png, 11-bluetooth-search.xml |
| 12. Busca Bluetooth e animações | Passou — comparação de 11-bluetooth-search-a/b.png |
| 13. ConnectedPage pelo Bluetooth e retorno | Passou — botão voltar do Android retorna à busca Bluetooth |
| 14. Histórico, calendário, + e horários | Passou — novo registro salvo em cada fluxo, horário real do dispositivo, histórico Wi-Fi visível pelo Bluetooth; calendário abre, filtra outra data e restaura hoje |

Também passaram: voltar do Android fecha primeiro o calendário; seleção Bluetooth preservada no retorno; busca Wi-Fi e arcos retomam após voltar da ConnectedPage e após segundo plano.

[Comparações de animação](animation-results.json), [verificador de capturas](verify_animation.py). Nas quatro comparações, os dois arcos mudaram entre quadros e o centro do símbolo permaneceu idêntico.

Capturas principais:

- [Tela inicial Bluetooth](01-home-initial.png)
- [Tela inicial Wi-Fi](02-wifi-selected.png)
- [Busca Wi-Fi](03-wifi-search-a.png)
- [Conectada pelo Wi-Fi](04-wifi-connected.png)
- [Alerta Wi-Fi](05-wifi-alert.png)
- [Calendário Wi-Fi](06-wifi-calendar.png)
- [Busca Bluetooth](11-bluetooth-search-a.png)
- [Alerta Bluetooth](13-bluetooth-alert.png)
- [Calendário Bluetooth](14-bluetooth-calendar.png)

## Limitações

A conexão e a descoberta permanecem simuladas. Não foi implementado tráfego de rede, descoberta ESP32 ou conexão BLE real.

Os testes ocorreram no emulador, sem aparelho físico ou pulseira. O emulador apresentou lentidão, avisos de sistema sem resposta e leituras temporariamente inválidas do UI Automator. Após recuperação e espera pela renderização, todos os testes finais passaram. Não foi feita medição de fluidez em hardware físico.

Os dois testes do botão + acrescentaram dois alertas simulados aos dados do emulador, sem apagar dados existentes.
