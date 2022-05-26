# Gnoss.Web.Results.OpenCORE

Aplicación Web que se encarga de mostrar los resultados en una página de búsqueda. Los resultados pueden ser recursos, instancias de objetos de conocimiento, personas, grupos, etc. 

Configuración estandar de esta aplicación en el archivo docker-compose.yml: 

```yml
resultados:
    image: results
    env_file: .env
    ports:
     - ${puerto_resultados}:80
    environment:
     virtuosoConnectionString: ${virtuosoConnectionString}
     acid: ${acid}
     base: ${base}
     redis__redis__ip__master: ${redis__redis__ip__master}
     redis__redis__ip__read: ${redis__redis__ip__read}
     redis__redis__bd: ${redis__redis__bd}
     redis__redis__timeout: ${redis__redis__timeout}
     redis__recursos__ip__master: ${redis__recursos__ip__master}
     redis__recursos__ip__read: ${redis__recursos__ip__read}
     redis__recursos__bd: ${redis__recursos__bd}
     redis__recursos__timeout: ${redis__redis__timeout}
     idiomas: ${idiomas}
     Servicios__urlBase: ${Servicios__urlBase__web}
     connectionType: ${connectionType}
    volumes:
      - ./logs/resultados:/app/logs
      - ./logs/resultados:/app/trazas
```

Se pueden consultar los posibles valores de configuración de cada parámetro aquí: https://github.com/equipognoss/Gnoss.Platform.Deploy
