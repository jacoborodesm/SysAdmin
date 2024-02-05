Índice de ejemplos:
  1. ServicePortmon: servicio de Windows destinado a la escucha de puertos del sistema. En caso de que algún puerto esté parado reinicia el servicio indicado en el config.xml definido.
     1.1 config.xml
         ##<Configuration>
         ##  <ServerAddress>localhost</ServerAddress>
         ##  <PortstoCheck>80,443</PortstoCheck>
         ##  <ServiceToRestart>Apache</ServiceToRestart>
         ##</Configuration>
