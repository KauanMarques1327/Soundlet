# Soundlet no Android

Diagnóstico e execução em 24/10/2026.

## Resultado verificado

- SDK .NET 10.0.401, workload MAUI 10.0.110, Android SDK/API 36 e build-tools 36.0.0 instalados.
- A extensão MAUI 1.17.289 usa o JDK 21.0.12.1 configurado nas preferências do VS Code. O Java 17 encontrado no PATH não foi usado na compilação.
- AVD `MAUI_Emulator_API_36`, arquitetura x86_64, com WHPX disponível.
- O ADB inicialmente mostrou `emulator-5554 offline`. As consultas de boot e ABI falharam nesse estado, explicando a impossibilidade de obter a ABI no XA0010.
- Reiniciar apenas o ADB não resolveu. A instância travada foi encerrada e o mesmo AVD iniciou sem carregar snapshot, preservando seus dados.
- Depois do boot: `sys.boot_completed=1`, ABI `x86_64,arm64-v8a`, API 36 e estado `device`.
- A compilação Debug explícita para `android-x64` terminou com zero erros e seis avisos CS8622/CA1416. Log: `android-build.log`.
- As duas tarefas de depuração MAUI também terminaram com código zero; o pacote `com.companyname.soundlet` foi instalado com Fast Deployment.
- O aplicativo abriu por ADB e renderizou a tela inicial. Na última verificação, PID `2254` e `MainActivity` em primeiro plano.

## Limitação ainda presente

O próprio Android apresentou watchdog, avisos de processo do sistema/Pixel Launcher sem resposta e timeout na inicialização de processos. As duas sessões do depurador encerraram. O computador tem aproximadamente 8 GB de RAM e foram observados cerca de 900 MB livres; isso pode contribuir para a lentidão, mas não comprova sozinho sua causa.

Um novo boot sem snapshot terminou em aproximadamente 120 segundos. O teste com `-memory 1536` não reduziu a memória efetiva: o emulador manteve 2560 MB. Não foi feita alteração persistente de RAM no AVD.

XAML Hot Reload está habilitado na configuração, mas **não foi validado em execução**: não foi possível manter o depurador conectado. Nenhum XAML foi alterado para simular uma validação.

## Próximas sessões

1. Abra esta pasta raiz `C:\Users\kauan\Desktop\Prose\Soundlet` no VS Code.
2. Inicie/selecione `MAUI_Emulator_API_36` pelo seletor de dispositivos MAUI. Aguarde o Android ficar utilizável antes de depurar. A conexão ADB deve estar em `device` e `sys.boot_completed` deve retornar `1`.
3. Em Run and Debug, selecione **Soundlet - Android Debug** e pressione **F5** uma vez.
4. Mantenha a sessão conectada enquanto edita a interface. Com o app aberto fora do depurador, alterações XAML não são aplicadas por esse fluxo.
5. Quando a sessão ficar estável, navegue até a página Bluetooth. Em `BluetoothPage.xaml`, altere temporariamente `CentralLoading.Color` de `#00F0FF` para `#FF00FF` e salve. Confirme a mudança no emulador sem reiniciar o processo e depois restaure `#00F0FF`.
6. Se a sessão encerrar ou o Android exibir novos avisos de sistema sem resposta, esse teste ainda não comprova Hot Reload. Libere memória fechando aplicativos que você não estiver usando antes de tentar novamente.

O perfil usa um caminho absoluto para o projeto: esta versão da extensão consultou o projeto antes de expandir `${workspaceFolder}` e cancelou a primeira tentativa com `startup project not found`.

Para abrir somente o aplicativo já instalado, use seu ícone no emulador. Isso não inicia uma sessão de Hot Reload.

## Alterações

Criados `.vscode/launch.json` e `.vscode/settings.json`, além deste relatório e dos logs/capturas de diagnóstico. Nenhuma tela, imagem, navegação, funcionalidade ou arquivo de código do projeto foi modificado. Nenhum SDK, pacote ou workload foi atualizado; não houve wipe data, exclusão de AVD ou operação destrutiva de Git.

Referência: [XAML Hot Reload para MAUI](https://learn.microsoft.com/en-us/dotnet/maui/xaml/hot-reload) requer configuração Debug e depurador conectado e oferece suporte ao VS Code com Android.

## Correção do comando Run em 26/10/2026

Foi reproduzido ADB0020 / IncompatibleCpuAbiException ao executar na raiz:

```powershell
dotnet build -t:Run -f net10.0-android
```

O APK incremental continha somente x86_64, gerado para o emulador. Com um Motorola ARM64 e o emulador conectados, Run escolheu o aparelho físico e falhou na instalação. O SDK usa uma arquitetura específica no Fast Deployment e havia cache do destino anterior.

Em Soundlet/Soundlet.csproj, EmbedAssembliesIntoApk passou a true somente para Debug/net10.0-android. Isso mantém as duas arquiteturas padrão (android-arm64 e android-x64) no empacotamento normal, incorporando as assemblies ao APK e evitando essa dependência do pacote de Fast Deployment anterior. Release e as demais plataformas não foram alterados. A primeira geração/transferência do pacote Debug pode ser maior.

O comando acima pode ser usado na raiz desta pasta ou na pasta Soundlet que contém o csproj, com um dispositivo Android disponível. Quando quiser escolher explicitamente um destino entre vários conectados:

```powershell
dotnet build -t:Run -f net10.0-android "-p:AdbTarget=-s emulator-5554"
```

O serial pode ser consultado com adb devices. O projeto não fixa o serial do celular nem do emulador.

Validação da correção:

- Comando original executado na raiz: sucesso, código de saída 0; três avisos CA1416 preexistentes.
- Diagnóstico do SDK confirma instalação com deploy.result=Success no Motorola moto g35 5G (ARM64).
- APK resultante verificado: arm64-v8a e x86_64, com assemblies incorporadas.
- Após desconectar o celular, repetição de Run com log normal: sucesso, zero erros, destino emulator-5554; novo processo Soundlet iniciado. Execução incremental: 25,97 segundos.
- Logs e evidências: artifacts/android-run/run-fixed.log, run-emulator.log, phone-deploy.json e apk-validation.json.

Na conferência de inicialização do emulador houve um aviso temporário de aplicativo sem resposta, coerente com a lentidão já documentada neste ambiente. Após Aguardar e encerrar os servidores de compilação, a MainActivity voltou ao primeiro plano. O log do processo não mostrou exceção fatal. A renderização da MainPage foi verificada pelos controles Bluetooth e Wi-Fi na árvore de acessibilidade.
