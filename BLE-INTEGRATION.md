# Integração BLE Android

O Bluetooth utiliza `Plugin.BLE` **3.2.1**, registrado por `BluetoothService` como singleton no `MauiProgram`. As páginas são resolvidas pelo contêiner do Shell. O transporte Wi-Fi continua simulado e seus arquivos não foram alterados.

## Protocolo

| Campo | Valor |
|---|---|
| Nome anunciado | `Soundlet Pulseira` |
| Serviço GATT | `4fafc201-1fb5-459e-8fcc-c5c9c331914b` |
| Característica READ/NOTIFY | `beb5483e-36e1-4688-b7f5-ea07361b26a8` |
| Notificação | `PALMA`, em UTF-8 |

A descoberta reconhece o nome ou o UUID de serviço de 128 bits anunciado, sem MAC fixo. Resultados são deduplicados pelo identificador do dispositivo durante a busca de 15 segundos. O cartão embutido na imagem fica encoberto até existir um resultado real. Sem resultados, é possível repetir a busca.

O toque interrompe a busca, conecta, descobre serviço/característica e aguarda `StartUpdatesAsync` antes de navegar. A tentativa tem limite de 25 segundos; falhas mantêm o usuário na busca. Há proteção contra toques e buscas simultâneos. Uma conexão já ativa é reutilizada ao retornar à busca.

Cada callback `ValueUpdated` com `PALMA` gera uma chamada ao mesmo `AlertHistory.AddAsync` usado pelo simulador. Não há leitura inicial do valor READ, pois um valor antigo não comprova uma nova palma. O horário é capturado com `DateTime.Now` no recebimento; a tela conserva `HH:mm` e o calendário existente. Mensagens desconhecidas são ignoradas. Terminadores de linha, espaços externos e NUL são tolerados.

O novo `AlertType.Clap` é acrescentado ao enum sem renumerar os tipos existentes. O JSON persiste `Source=Bracelet` para BLE e `Source=Manual` para o botão `+`. Arquivos antigos, sem `Source`, continuam legíveis como registros manuais. Notificações distintas com o mesmo conteúdo e horário são preservadas como eventos distintos.

O serviço continua recebendo e salvando eventos durante a navegação. As páginas assinam eventos somente enquanto visíveis. A perda de conexão ou desativação do Bluetooth remove o estado conectado e a assinatura local; a tela informa a desconexão e permite voltar à busca. `DisconnectAsync` encerra notificações e conexão explicitamente, e `DisposeAsync` também remove os handlers globais. Não há reconexão automática, serviço Android em primeiro plano ou garantia de recepção com o aplicativo encerrado. Esta etapa destina-se ao Android com o aplicativo aberto.

## Permissões

- Android 12+: `BLUETOOTH_SCAN` com `neverForLocation` e `BLUETOOTH_CONNECT`, solicitadas em tempo de execução.
- Até Android 11: `BLUETOOTH` e `BLUETOOTH_ADMIN`, limitadas a API 30; `ACCESS_FINE_LOCATION`, também limitada a API 30, solicitada em tempo de execução a partir da API 23. Nesses sistemas a localização precisa estar ativada para a busca.
- BLE é declarado como recurso opcional; a disponibilidade é verificada pelo serviço.
- Não foi adicionada permissão de anúncio nem localização em segundo plano.

As chamadas foram conferidas no XML do pacote instalado e nos [contratos da versão 3.2.1](https://github.com/dotnet-bluetooth-le/dotnet-bluetooth-le/tree/3.2.1/Source/Plugin.BLE/Shared/Contracts). A configuração de permissões segue a [documentação Android](https://developer.android.com/develop/connectivity/bluetooth/bt-permissions).

## Validação

Comandos utilizados:

```powershell
dotnet build -f net10.0-android
dotnet run --project artifacts/connected/checks/Checks.csproj
& "$env:LOCALAPPDATA/Android/Sdk/platform-tools/adb.exe" devices -l
```

O build Android terminou com zero erros e três avisos CA1416 preexistentes em `MainActivity.cs`. Os 155 checks passaram e cobrem o calendário, os seis tipos manuais, persistência, inclusões concorrentes de palmas, identificação da origem e leitura do JSON anterior. Eles não exercitam o rádio BLE nem a interface Android. Logs locais: `artifacts/ble-build.log`, `artifacts/ble-build-final.log` e `artifacts/ble-history-checks.log`.

O ADB não encontrou aparelhos conectados nesta sessão. Portanto, a conexão real com ESP32-S3 e os fluxos visuais no aparelho **não foram validados**.

Roteiro pendente no celular:

1. Fechar/desconectar o nRF Connect para liberar a pulseira e ligar seu firmware existente.
2. Abrir Bluetooth no Soundlet, negar as permissões e conferir a mensagem; depois concedê-las e repetir a busca.
3. Com a pulseira desligada, conferir ausência do cartão e opção de nova busca. Testar também Bluetooth desligado.
4. Ligar a pulseira, repetir a busca e confirmar um único cartão `Soundlet Pulseira`.
5. Tocar no cartão, inclusive repetidamente, e confirmar navegação somente após a assinatura GATT.
6. Produzir uma palma e conferir um alerta `Palma detectada`, descrição e horário local. Repetir várias vezes e verificar um evento por notificação efetivamente enviada pelo firmware.
7. Usar `+`, abrir o calendário, filtrar datas e reabrir o aplicativo para conferir persistência e animações.
8. Desligar ou afastar a pulseira, conferir o estado desconectado, voltar à busca e conectar novamente.
9. Navegar com a conexão ativa e confirmar que novos eventos continuam no histórico, sem multiplicação de handlers.
10. Abrir o fluxo Wi-Fi e confirmar sua navegação e simulação anteriores.

O aplicativo não altera o firmware e não classifica áudio: interpreta exclusivamente as notificações enviadas pela pulseira.

## Arquivos desta implementação

- `Soundlet/Services/BluetoothService.cs`: descoberta, conexão, assinatura, recepção e desconexão.
- `Soundlet/Platforms/Android/BlePermissions.cs` e `AndroidManifest.xml`: permissões por versão do Android.
- `Soundlet/MauiProgram.cs` e `Soundlet/Soundlet.csproj`: injeção de dependências e pacote BLE.
- `Soundlet/BluetoothPage.xaml` e `.xaml.cs`: busca real e cartão condicionado à descoberta.
- `Soundlet/ConnectedPage.xaml` e `.xaml.cs`: estado real e apresentação dos alertas.
- `Soundlet/Models/AlertRecord.cs` e `Soundlet/Services/AlertHistory.cs`: palma e origem persistida.
- `artifacts/connected/checks/Program.cs`: verificações adicionais do histórico.
- `BLE-INTEGRATION.md`: protocolo, implementação e roteiro de validação.
