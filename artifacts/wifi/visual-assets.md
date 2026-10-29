# Recursos visuais

Ferramenta: imagegen integrada (sem CLI/API externa).

- Soundlet/Resources/Images/soundlet_home_background.png: cópia do fundo inicial sem os dois botões, para desenhar estados selecionáveis com gráficos nativos. Original soundlet_home.png preservado.
- Soundlet/Resources/Images/soundlet_wifi.png: referência Wi-Fi reproduzida em 850 × 1850. Original soundlet_search.png preservado.

Prompt do fundo:
Use case: precise-object-edit. Edit this Soundlet mobile app artwork. Remove ONLY both connection button pills (Bluetooth and Wi-Fi), including their text, icons, arrows, borders and cyan glow, located x=90..750 y=1150..1490 in the original 850x1850 image. Seamlessly restore the dark navy background underneath those two buttons. Keep ALL other pixels visually unchanged: logo, Soundlet wordmark, welcome text, curved yellow/cyan neon decorative edges, full composition. Exact same portrait aspect ratio 850:1850. No new elements. This is a production background with real UI controls to be placed in the emptied region. Save output image to disk.

Prompt da busca:
Use case: precise-object-edit. Create production Soundlet Wi-Fi search screen artwork, matching the USER'S ATTACHED WI-FI SCREEN exactly. Of the three supplied images, the USER image is the target Wi-Fi artwork, the other images are existing home and Bluetooth screens. Reproduce target USER Wi-Fi artwork with maximum fidelity at aspect ratio 850:1850 portrait: navy black background identical to Bluetooth, same decorative cyan blue/yellow curves, same yellow back at (38,114)-(132,208), Wi-Fi cyan glyph bounds (311,486)-(549,676), title 'Procurando pulseira...' at y710, descriptive text exact 'Mantenha sua pulseira e seu aparelho' line1 y792, 'conectados a mesma rede Wi-Fi.' line2 y834. Heading 'Dispositivos encontrados' at (58,933), device card (57,985)-(793,1134) same bracelet icon and 'Soundlet Pulseira', 'ESP32-S3', cyan right arrow. No loading circles: app draws animated ones. Match typography, proportions, spacing, gradients. Do not add elements. Save output to disk.
