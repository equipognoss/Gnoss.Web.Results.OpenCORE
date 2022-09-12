![](https://content.gnoss.ws/imagenes/proyectos/personalizacion/7e72bf14-28b9-4beb-82f8-e32a3b49d9d3/cms/logognossazulprincipal.png)

# Gnoss.Web.Results.OpenCORE

![](https://github.com/equipognoss/Gnoss.Web.Results.OpenCORE/workflows/BuildResults/badge.svg)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=equipognoss_Gnoss.Web.Results.OpenCORE&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=equipognoss_Gnoss.Web.Results.OpenCORE)
[![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=equipognoss_Gnoss.Web.Results.OpenCORE&metric=duplicated_lines_density)](https://sonarcloud.io/summary/new_code?id=equipognoss_Gnoss.Web.Results.OpenCORE)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=equipognoss_Gnoss.Web.Results.OpenCORE&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=equipognoss_Gnoss.Web.Results.OpenCORE)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=equipognoss_Gnoss.Web.Results.OpenCORE&metric=bugs)](https://sonarcloud.io/summary/new_code?id=equipognoss_Gnoss.Web.Results.OpenCORE)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=equipognoss_Gnoss.Web.Results.OpenCORE&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=equipognoss_Gnoss.Web.Results.OpenCORE)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=equipognoss_Gnoss.Web.Results.OpenCORE&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=equipognoss_Gnoss.Web.Results.OpenCORE)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=equipognoss_Gnoss.Web.Results.OpenCORE&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=equipognoss_Gnoss.Web.Results.OpenCORE)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=equipognoss_Gnoss.Web.Results.OpenCORE&metric=sqale_index)](https://sonarcloud.io/summary/new_code?id=equipognoss_Gnoss.Web.Results.OpenCORE)

Aplicación Web que se encarga de mostrar los resultados en una página de búsqueda. Los resultados pueden ser recursos, instancias de objetos de conocimiento, personas, grupos, etc. 

Configuración estandar de esta aplicación en el archivo docker-compose.yml: 

```yml
results:
    image: gnoss/gnoss.web.results.opencore
    env_file: .env
    ports:
     - ${puerto_results}:80
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
      - ./logs/results:/app/logs
      - ./logs/results:/app/trazas
```

Se pueden consultar los posibles valores de configuración de cada parámetro aquí: https://github.com/equipognoss/Gnoss.SemanticAIPlatform.OpenCORE

## Código de conducta
Este proyecto a adoptado el código de conducta definido por "Contributor Covenant" para definir el comportamiento esperado en las contribuciones a este proyecto. Para más información ver https://www.contributor-covenant.org/

## Licencia
Este producto es parte de la plataforma [Gnoss Semantic AI Platform Open Core](https://github.com/equipognoss/Gnoss.SemanticAIPlatform.OpenCORE), es un producto open source y está licenciado bajo GPLv3.
