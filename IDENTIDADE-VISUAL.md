# Soundlet — identidade visual e navegação

## Implementação

- `Soundlet/App.xaml.cs`: a janela começa em `SplashPage`; o Shell existente continua responsável pelas três telas do aplicativo.
- `Soundlet/SplashPage.xaml` e `.xaml.cs`: composição oficial sem barra estática, apresentação de 1,5 s e fade de 180 ms. O histórico existente é lido por `AlertHistory.LoadAsync` enquanto a interface anima. O trecho final da barra aguarda essa inicialização. A barra é uma apresentação visual, sem porcentagem ou alegação de medir trabalho real. Ao terminar, `Window.Page` recebe `AppShell`, descartando a abertura da navegação.
- `Soundlet/SplashProgressDrawable.cs`: barra ciano com cantos arredondados e brilho. Coordenadas originais `(293,1247,274,8)` transformadas pela mesma escala e recorte central `AspectFill` da composição de 850×1850.
- `Soundlet/Resources/Images/soundlet_splash.png`: imagem da abertura preparada a partir da referência, com remoção da barra estática e seu brilho.
- `Soundlet/Resources/AppIcon/appicon.svg`: fundo `#0B132B`. `soundlet_symbol.png`, em AppIcon e Splash: símbolo azul/ciano adaptado para PNG transparente e centralizado, mantendo proporções.
- `Soundlet/Soundlet.csproj`: ícone adaptativo com `ForegroundScale=0.85`, splash nativa azul-marinho e símbolo Soundlet. `ApplicationTitle` continua `Soundlet`. O Resizetizer gera as densidades Android, recursos redondos e adaptativos. Os antigos SVGs roxos do foreground e splash foram removidos.
- `Soundlet/BluetoothPage.xaml` e `.xaml.cs`: botão transparente dentro do viewport da arte, com prioridade de toque e limites originais `(57,985,736,149)`. Usa exatamente o mapeamento dos indicadores, inclusive o recorte `AspectFill`. Os indicadores e seus ciclos de animação permanecem iguais.
- `Soundlet/ConnectedPage.xaml`: somente o botão de voltar mudou; mantém 44×44, estilo, cores e evento. O glifo dependente da linha de base da fonte foi substituído por um único chevron geométrico centralizado em `(22,22)` e transparente a toques.

Nenhum pacote foi adicionado ao aplicativo. BLE continua simulado. Os arquivos de histórico, calendário, modelos e simulação não foram modificados.

## Recursos e reprodução

A skill imagegen foi usada para preparar as imagens. O símbolo foi limpo e adaptado a partir da referência raster; não se trata de um vetor mestre fornecido pelo usuário. A fonte de adaptação está em `artifacts/identity/icon-source.png`; `artifacts/prepare_identity_assets.ps1` reproduz o recorte, conversão e centralização. O foreground possui 512×512 pixels, com símbolo limitado a 310 pixels e transparência ao redor. A verificação dos pixels opacos, após a escala de 0,85, resultou em raio máximo de 32,863 dp no canvas adaptativo de 108 dp, dentro do raio seguro de 33 dp.

`artifacts/verify_identity.py` contém os passos ADB de abertura e aceitação da interface. As ferramentas Python de vídeo usadas na verificação são externas ao aplicativo.

## Referências de configuração

- [Ícones MAUI e foreground adaptativo](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/images/app-icons?view=net-maui-10.0)
- [Splash nativa MAUI](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/images/splashscreen?view=net-maui-10.0)

## Validação

### Executado com sucesso

- Compilação `net10.0-android`, Debug, runtime `android-x64`: **zero erros, quatro avisos preexistentes** (CS8622 em MainPage e CA1416 em MainActivity). Log: `artifacts/identity-build-final.log`. Duração: 4 min 55 s. JDK 21 e SDK Android existentes, sem atualização de dependências.
- APK atualizado instalado no emulador API 36: `adb install -r` retornou `Success`. APK: `Soundlet/bin/Debug/net10.0-android/android-x64/com.companyname.soundlet-Signed.apk`. Este APK é **x86_64 para o emulador**, não um instalador ARM64 para o Motorola.
- Metadados do APK confirmados por `aapt2 dump badging`: nome `Soundlet`, ícones adaptativos e densidades 160/240/320/480/640. Evidência: `artifacts/identity-apk-metadata.txt`.
- Inspeção da imagem redonda gerada pelo Resizetizer: símbolo inteiro, centralizado, sobre azul-marinho. A configuração gerada da splash aponta para `@drawable/soundlet_symbol`.
- Executado `dotnet run --project artifacts/connected/checks/Checks.csproj`: todas as verificações existentes passaram. Incluem calendário para anos 1, 1900, 2000, 2024, 2026 e 9999, alinhamento dos dias da semana, seis tipos de alerta, IDs únicos, persistência JSON preservando horários/tipos, ordenação e filtro de data vazio. Log: `artifacts/identity-regression-checks.log`.

Comando da compilação aprovada:

```powershell
dotnet build Soundlet/Soundlet.csproj -f net10.0-android -r android-x64 -c Debug -p:JavaSdkDirectory=C:\Users\kauan\AppData\Local\Android\jdk\jdk-21\jdk-21.0.12.1+1 -p:AndroidSdkDirectory=C:\Users\kauan\AppData\Local\Android\Sdk -p:EmbedAssembliesIntoApk=true -m:1
```

### Limitações dos testes em execução

O aplicativo foi iniciado no emulador e a MainPage apareceu ao fundo na primeira captura, porém o próprio Android apresentou **Pixel Launcher isn't responding** e depois **System UI isn't responding**. As gravações ficaram bloqueadas por esses diálogos. Houve uma tentativa de reiniciar somente o Android, preservando os dados do AVD; mesmo após `sys.boot_completed=1`, a automação continuou instável, com falha de transferência ADB e término do UI Automator com código **137**. A máquina apresentou menos de 1 GB de RAM livre durante as tentativas. A causa exata dessa instabilidade não foi determinada; não é possível atribuí-la apenas à memória.

Evidências: `artifacts/identity/current.png` (MainPage sob aviso do Pixel Launcher), `artifacts/identity/main.png` e `startup-review.png` (aviso do System UI), `after-reboot.png` (launcher ainda sem renderização normal), `artifacts/identity-ui-tests.log` e `artifacts/identity-navigation-tests.log`.

Consequentemente, **não foram aprovados os testes visuais/interativos** de: splash nativa sem roxo em execução; ícone no launcher; ordem SplashPage/MainPage; barra começando vazia, animando e completando; fade e retorno sem SplashPage; oito regiões do cartão; seta centralizada; indicadores Bluetooth; histórico, animação de chegada, botão + e calendário pela interface. Esses comportamentos estão implementados, mas sua validação em execução permanece pendente. As verificações de lógica do histórico/calendário passaram separadamente, como descrito acima.

O Moto G35 5G não estava conectado e não foi testado. Nenhuma compilação ARM64 ou instalação no Motorola foi declarada concluída. Os frameworks, identificador, SDK mínimo/alvo e configuração nativa do Android foram preservados. A compilação ampla inicial foi interrompida durante o empacotamento; o resultado aprovado é especificamente a compilação `android-x64` indicada acima.
