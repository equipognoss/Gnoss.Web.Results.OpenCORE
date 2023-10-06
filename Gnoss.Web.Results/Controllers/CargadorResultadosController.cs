using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.EncapsuladoDatos;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.EntityModel.Models.Faceta;
using Es.Riam.Gnoss.AD.EntityModel.Models.ProyectoDS;
using Es.Riam.Gnoss.AD.EntityModel.Models.VistaVirtualDS;
using Es.Riam.Gnoss.AD.EntityModelBASE;
using Es.Riam.Gnoss.AD.Facetado;
using Es.Riam.Gnoss.AD.Facetado.Model;
using Es.Riam.Gnoss.AD.MetaBuscadorAD;
using Es.Riam.Gnoss.AD.Parametro;
using Es.Riam.Gnoss.AD.ParametroAplicacion;
using Es.Riam.Gnoss.AD.ServiciosGenerales;
using Es.Riam.Gnoss.AD.Usuarios;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.CL.Facetado;
using Es.Riam.Gnoss.CL.ParametrosProyecto;
using Es.Riam.Gnoss.CL.Seguridad;
using Es.Riam.Gnoss.CL.ServiciosGenerales;
using Es.Riam.Gnoss.CL.Tesauro;
using Es.Riam.Gnoss.CL.Trazas;
using Es.Riam.Gnoss.Elementos.CMS;
using Es.Riam.Gnoss.Elementos.Documentacion;
using Es.Riam.Gnoss.Elementos.Identidad;
using Es.Riam.Gnoss.Elementos.Tesauro;
using Es.Riam.Gnoss.Logica.Documentacion;
using Es.Riam.Gnoss.Logica.Facetado;
using Es.Riam.Gnoss.Logica.ServiciosGenerales;
using Es.Riam.Gnoss.Recursos;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.UtilServiciosWeb;
using Es.Riam.Gnoss.Web.Controles;
using Es.Riam.Gnoss.Web.MVC.Controles.Controladores;
using Es.Riam.Gnoss.Web.MVC.Models;
using Es.Riam.Gnoss.Web.MVC.Models.Administracion;
using Es.Riam.Util;
using Gnoss.Web.Services.VirtualPathProvider;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ServicioCargaResultados
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors("_myAllowSpecificOrigins")]
    public class CargadorResultadosController : ControllerBase
    {

        #region Constatnes

        /// <summary>
        /// Texto que se devuelve cuando no hay resultados
        /// </summary>
        private const string TEXTO_NO_RESULTADOS = "###ejecutarFuncion###WebForm_DoCallback('__Page','MostrarSinResultados',ReceiveServerData,'',null,false)###ejecutarFuncion###";

        /// <summary>
        /// Texto que se devuelve cuando no hay resultados y es petición gadget.
        /// </summary>
        private const string TEXTO_NO_RESULTADOS_GADGET = "SinResultados";

        #endregion

        #region Miembros

        /// <summary>
        /// Listado de elementos JSON
        /// </summary>
        private JsonListado mListadoJson = new JsonListado();

        /// <summary>
        /// ID de la organización
        /// </summary>
        private Guid mOrganizacionID = Guid.Empty;

        /// <summary>
        /// ID del usuario.
        /// </summary>
        private Guid mUsuarioID = Guid.Empty;

        /// <summary>
        /// UtilIdioimas
        /// </summary>
        private UtilIdiomas mUtilIdiomas;

        TrazaGnossWeb mTraza = null;

        /// <summary>
        /// Fila de la pestanya actual
        /// </summary>
        private ProyectoPestanyaMenu mFilaPestanyaActual = null;

        /// <summary>
        /// Obtiene si el ecosistema tiene una personalizacion de vistas
        /// </summary>
        private Guid? mPersonalizacionEcosistemaID = null;
        private bool? mComunidadExcluidaPersonalizacionEcosistema = null;

        private CargadorResultadosModel mCargadorResultadosModel;

        #region Variables del buscador facetado

        private string mBaseUrl = "";
        private string mBaseURLStatic = "";

        /// <summary>
        /// Obtiene la lista de URLs de los elementos no estaticos de la página
        /// </summary>
        private List<string> mBaseURLsContent;

        private EntityContext mEntityContext;
        private LoggingService mLoggingService;
        private RedisCacheWrapper mRedisCacheWrapper;
        private ConfigService mConfigService;
        private VirtuosoAD mVirtuosoAD;
        private GnossCache mGnossCache;
        private UtilServicios mUtilServicios;
        private IHttpContextAccessor mHttpContextAccessor;
        private EntityContextBASE mEntityContextBASE;
        private ICompositeViewEngine mViewEngine;
        private UtilServicioResultados mUtilServicioResultados;
        private UtilServiciosFacetas mUtilServiciosFacetas;
        private ControladorBase mControladorBase;
        private IHostingEnvironment mEnv;
        private IServicesUtilVirtuosoAndReplication mServicesUtilVirtuosoAndReplication;

        #endregion

        #endregion

        #region Constructor

        public CargadorResultadosController(EntityContext entityContext, LoggingService loggingService, RedisCacheWrapper redisCacheWrapper, ConfigService configService, VirtuosoAD virtuosoAD, GnossCache gnossCache, UtilServicios utilServicios, IHttpContextAccessor httpContextAccessor, EntityContextBASE entityContextBASE, ICompositeViewEngine viewEngine, UtilServicioResultados utilServicioResultados, UtilServiciosFacetas utilServiciosFacetas, IHostingEnvironment env, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication)
            : base(loggingService, configService, entityContext, redisCacheWrapper, gnossCache, virtuosoAD, httpContextAccessor, servicesUtilVirtuosoAndReplication)
        {
            mEntityContext = entityContext;
            mLoggingService = loggingService;
            mRedisCacheWrapper = redisCacheWrapper;
            mConfigService = configService;
            mVirtuosoAD = virtuosoAD;
            mGnossCache = gnossCache;
            mUtilServicios = utilServicios;
            mHttpContextAccessor = httpContextAccessor;
            mEntityContextBASE = entityContextBASE;
            mViewEngine = viewEngine;
            mUtilServicioResultados = utilServicioResultados;
            mUtilServiciosFacetas = utilServiciosFacetas;
            mEnv = env;
            mServicesUtilVirtuosoAndReplication = servicesUtilVirtuosoAndReplication;
            mControladorBase = new ControladorBase(loggingService, configService, entityContext, redisCacheWrapper, gnossCache, virtuosoAD, httpContextAccessor, servicesUtilVirtuosoAndReplication);
            mCargadorResultadosModel = new CargadorResultadosModel(entityContext, loggingService, redisCacheWrapper, configService, virtuosoAD, mServicesUtilVirtuosoAndReplication);
        }

        #endregion

        #region Metodos Web
        [HttpGet, HttpPost]
        [Route("LimpiarCache")]
        public ActionResult LimpiarCache()
        {
            //TODO Javier esta clase esta en Es.Riam.Gnoss.Web.MVC.Controles hay que migrarla
            //MyVirtualPathProvider.listaRutasVirtuales.Clear();
            VistaVirtualCL vistaVirtualCL = new VistaVirtualCL(mEntityContext, mLoggingService, mGnossCache, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
            vistaVirtualCL.InvalidarVistasVirtualesEcosistemaEnCacheLocal();
            return Content("OK");
        }

        [NonAction]
        private ActionResult CargarResultadosMapa(Guid pProyectoID, string parametros)
        {
            Guid documentoIDMapa = Guid.Empty;
            if (parametros.Contains("default;rdf:type=") || parametros.Contains("documentoid="))
            {
                string[] filtrosParametros = parametros.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

                parametros = "";
                foreach (string filtro in filtrosParametros)
                {
                    if (!filtro.StartsWith("documentoid="))
                    {
                        parametros += filtro + "|";
                    }
                    else
                    {
                        documentoIDMapa = new Guid(filtro.Substring(filtro.IndexOf("=") + 1));
                    }
                }
            }

            StringBuilder sb = new StringBuilder(ObtenerTextoNumResultados() + "|||");

            if (mCargadorResultadosModel.FacetadoDS.Tables.Contains("RecursosBusqueda"))
            {
                foreach (DataRow fila in mCargadorResultadosModel.FacetadoDS.Tables["RecursosBusqueda"].Rows)
                {
                    if (!string.IsNullOrEmpty(fila["lat"].ToString()) && !string.IsNullOrEmpty(fila["long"].ToString()))
                    {
                        if (!documentoIDMapa.Equals(Guid.Empty) && fila["s"].ToString().ToLower().Contains(documentoIDMapa.ToString().ToLower()))
                        {
                            documentoIDMapa = Guid.Empty;
                            sb.Append($"{fila["s"]},documentoid,{fila["lat"].ToString().Replace(',', '.')},{fila["long"].ToString().Replace(',', '.')}");
                        }
                        else
                        {
                            sb.Append($"{fila["s"]},{fila["rdftype"]},{fila["lat"].ToString().Replace(',', '.')},{fila["long"].ToString().Replace(',', '.')}");
                        }
                    }

                    if (fila.ItemArray.Length > 5 && mCargadorResultadosModel.FacetadoDS.Tables["RecursosBusqueda"].Columns.Contains("ruta") && string.IsNullOrEmpty(fila["ruta"].ToString()))
                    {
                        sb.Append($"Polyline,{fila["s"]},{fila["rdftype"]},geo:coordinates,{fila["ruta"].ToString().Replace(',', ';').Replace("{geo:coordinates:", "{\"geo:coordinates\":")},{fila["color"]}");
                    }

                    if (fila.ItemArray.Length > 4 && mCargadorResultadosModel.FacetadoDS.Tables["RecursosBusqueda"].Columns.Contains("prop0"))
                    {
                        for (int i = 0; i < fila.ItemArray.Length - 4; i++)
                        {
                            sb.Append($",{fila[$"prop{i}"].ToString().Replace(",", "#COMA#")}");
                        }
                    }
                    sb.Append("|||");
                }

                if (!documentoIDMapa.Equals(Guid.Empty))
                {
                    List<Guid> listaID = new List<Guid>();
                    listaID.Add(documentoIDMapa);

                    FacetaCN fac = new FacetaCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    DataWrapperFacetas facetaDW = new DataWrapperFacetas();
                    fac.CargarFacetaConfigProyMapa(mCargadorResultadosModel.Proyecto.FilaProyecto.OrganizacionID, mCargadorResultadosModel.Proyecto.FilaProyecto.ProyectoID, facetaDW);
                    fac.Dispose();


                    if (facetaDW.ListaFacetaConfigProyMapa.Count > 0)
                    {
                        List<string> listaPropiedades = new List<string>();
                        string latitud = facetaDW.ListaFacetaConfigProyMapa.FirstOrDefault().PropLatitud;
                        string longitud = facetaDW.ListaFacetaConfigProyMapa.FirstOrDefault().PropLongitud;
                        listaPropiedades.Add(latitud);
                        listaPropiedades.Add(longitud);

                        FacetadoCL facetadoCL = new FacetadoCL(mUtilServicios.UrlIntragnoss, mCargadorResultadosModel.AdministradorQuiereVerTodasLasPersonas, pProyectoID.ToString().ToLower(), true, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);

                        FacetadoDS facetadoDS = facetadoCL.FacetadoCN.ObtenerValoresPropiedadesEntidadesPorDocumentoID(pProyectoID.ToString(), listaID, listaPropiedades, "", true);

                        if (facetadoDS.Tables["SelectPropEnt"] != null)
                        {
                            DataRow[] rowLatitud = facetadoDS.Tables["SelectPropEnt"].Select($"p='{latitud}'");
                            DataRow[] rowLongitud = facetadoDS.Tables["SelectPropEnt"].Select($"p='{longitud}'");

                            if (rowLatitud.Length == 1 && rowLongitud.Length == 1)
                            {
                                sb.Append($"http://gnoss/{documentoIDMapa.ToString().ToUpper()},documentoid,{rowLatitud[0]["o"].ToString().Replace(',', '.')},{rowLongitud[0]["o"].ToString().Replace(',', '.')}|||");
                            }

                        }
                    }
                }
            }

            return Content(System.Text.Json.JsonSerializer.Serialize(sb.ToString()));
        }

        [NonAction]
        private ActionResult CargarResultadosChart()
        {
            string resultado = ObtenerTextoNumResultados() + "|||";
            int numCol = mCargadorResultadosModel.FacetadoDS.Tables["RecursosBusqueda"].Columns.Count;
            bool mapearTypeSubType = mCargadorResultadosModel.SelectFiltroChart.Value.Contains("rdf:type") || mCargadorResultadosModel.SelectFiltroChart.Value.Contains("gnoss:type");

            foreach (DataRow fila in mCargadorResultadosModel.FacetadoDS.Tables["RecursosBusqueda"].Rows)
            {
                for (int i = 0; i < numCol; i++)
                {
                    string valor;
                    try
                    {
                        valor = (string)fila[i];
                    }
                    catch (Exception e)
                    {
                        valor = "";
                    }

                    if (mapearTypeSubType)
                    {
                        valor = ObtenerValorMapeandoTypeSubType(valor);
                    }

                    resultado += valor + "@@@";
                }

                resultado += "|||";
            }

            return Content(System.Text.Json.JsonSerializer.Serialize(resultado));
        }
        [HttpGet, HttpPost]
        [Route("CargarResultados")]
        public ActionResult CargarResultados([FromForm] string pProyectoID, [FromForm] string pIdentidadID, [FromForm] bool pEsUsuarioInvitado, [FromForm] string pUrlPaginaBusqueda, [FromForm] bool pUsarMasterParaLectura, [FromForm] bool pAdministradorVeTodasPersonas, [FromForm] short pTipoBusqueda, [FromForm] string pGrafo, [FromForm] string pParametros_adiccionales, [FromForm] string pParametros, [FromForm] bool pPrimeraCarga, [FromForm] string pLanguageCode, [FromForm] int pNumeroParteResultados, [FromForm] string pFiltroContexto, [FromForm] bool? pJson, [FromForm] string tokenAfinidad, [FromForm] string pListaRecursosExcluidos)
        {
            try
            {
                // mLoggingService.GuardarLog($"ProyectoID: {pProyectoID} |||| pIdentidadID: {pIdentidadID} |||| pEsUsuarioInvitado: {pEsUsuarioInvitado} |||| pUrlPaginaBusqueda: {pUrlPaginaBusqueda}");
                //mLoggingService.GuardarLogError("Entra a CargarResultados");
                if (!string.IsNullOrEmpty(tokenAfinidad))
                {
                    new SeguridadCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication).ObtenerConexionAfinidad(tokenAfinidad.Replace("\"", ""));
                }
                if (pFiltroContexto == null) { pFiltroContexto = ""; }
                if (pParametros_adiccionales == null) { pParametros_adiccionales = ""; }
                mLoggingService.AgregarEntrada("Entra en CargarResultados");
                #region Obtenemos parámetros

                pProyectoID = pProyectoID.Replace("\"", "");
                pIdentidadID = pIdentidadID.Replace("\"", "");
                pGrafo = pGrafo?.Replace("\"", "");
                pParametros_adiccionales = pParametros_adiccionales.Replace("\"", "");
                if (string.IsNullOrEmpty(pParametros))
                {
                    pParametros = "";
                }
                if (pParametros.StartsWith("\"") && pParametros.EndsWith("\""))
                {
                    pParametros = pParametros.Substring(1, pParametros.Length - 2);
                }

                pLanguageCode = pLanguageCode.Replace("\"", "");
                pFiltroContexto = pFiltroContexto.Replace("\"", "");
                mCargadorResultadosModel.ListaRecursosExcluidos = ObtenerListaDeExcluidos(pListaRecursosExcluidos);
                mCargadorResultadosModel.LanguageCode = pLanguageCode;

                //Url desde la que ha llegado
                if (HttpContext.Request.Headers.ContainsKey("Referer"))
                {
                    mCargadorResultadosModel.UrlNavegador = Request.Headers["Referer"].ToString();
                }
                else if (!string.IsNullOrEmpty(pUrlPaginaBusqueda))
                {
                    pUrlPaginaBusqueda = new Uri(pUrlPaginaBusqueda.Replace("\"", "")).PathAndQuery;
                    mCargadorResultadosModel.UrlNavegador = pUrlPaginaBusqueda;
                }

                if (pFiltroContexto.Length > 2 && pFiltroContexto.StartsWith("\"") && pFiltroContexto.EndsWith("\""))
                {
                    pFiltroContexto = pFiltroContexto.Substring(1, pFiltroContexto.Length - 2);
                }

                Guid proyectoID = new Guid(pProyectoID);
                Guid identidadID = new Guid(pIdentidadID);

                mCargadorResultadosModel.ProyectoSeleccionado = proyectoID;

                #region Obtener solo IDs

                bool BusquedaSoloIDs = false;

                if (pParametros_adiccionales.Contains("busquedaSoloIDs=true"))
                {
                    BusquedaSoloIDs = true;
                    pParametros_adiccionales = pParametros_adiccionales.Replace("|busquedaSoloIDs=true", "");
                }

                if (pParametros_adiccionales.Contains("|numeroResultados="))
                {
                    string numResultados = pParametros_adiccionales.Substring(pParametros_adiccionales.IndexOf("|numeroResultados=") + "|numeroResultados=".Length);
                    if (numResultados.Contains("|"))
                    {
                        numResultados = numResultados.Substring(0, numResultados.IndexOf("|"));
                    }
                    mCargadorResultadosModel.NumeroResultadosMostrar = int.Parse(numResultados);
                    pParametros_adiccionales = pParametros_adiccionales.Replace("|numeroResultados=" + numResultados, "");
                }

                bool obtenerDatosExtraIdentidades = false;
                if (pParametros_adiccionales.Contains("|obtenerDatosExtraIdentidades=true"))
                {
                    obtenerDatosExtraIdentidades = true;
                    pParametros_adiccionales = pParametros_adiccionales.Replace("|obtenerDatosExtraIdentidades=true", "");
                }

                #endregion

                #endregion

                #region Buscamos resultados

                AsignarPropiedadesCargadorResultadosModel();

                bool esMovil = mControladorBase.RequestParams("esMovil") == "true";

                TipoResultadoBusqueda tipoResultadoBusqueda = mUtilServicioResultados.CargarResultadosInt(proyectoID, identidadID, identidadID != UsuarioAD.Invitado, pEsUsuarioInvitado, (TipoBusqueda)pTipoBusqueda, pGrafo, pParametros, pParametros_adiccionales, pPrimeraCarga, pLanguageCode, pNumeroParteResultados, mCargadorResultadosModel.FilasPorPagina, TipoFichaResultados.Completa, pFiltroContexto, pAdministradorVeTodasPersonas, mCargadorResultadosModel, esMovil, BusquedaSoloIDs);

                #endregion

                #region ProyectoVirtualID

                Guid proyectoVirtualID = proyectoID;

                if (pParametros_adiccionales.Contains("proyectoVirtualID="))
                {
                    string trozo1 = pParametros_adiccionales.Substring(0, pParametros_adiccionales.IndexOf("proyectoVirtualID="));
                    string trozoProyOrgien = pParametros_adiccionales.Substring(pParametros_adiccionales.IndexOf("proyectoVirtualID="));
                    string trozo2 = trozoProyOrgien.Substring(trozoProyOrgien.IndexOf("|") + 1);
                    trozoProyOrgien = trozoProyOrgien.Substring(0, trozoProyOrgien.IndexOf("|"));

                    proyectoVirtualID = new Guid(trozoProyOrgien.Substring(trozoProyOrgien.IndexOf("=") + 1));
                    pParametros_adiccionales = trozo1 + trozo2;
                }

                #endregion

                string funcionCallBack = HttpContext.Request.Query["callback"];
                if (tipoResultadoBusqueda == TipoResultadoBusqueda.Correcto)
                {
                    if (pParametros_adiccionales.Contains("busquedaTipoMapa=true"))
                    {
                        return CargarResultadosMapa(proyectoID, pParametros);
                    }
                    else if (pParametros_adiccionales.Contains("busquedaTipoChart="))
                    {
                        return CargarResultadosChart();
                    }

                    if (BusquedaSoloIDs)
                    {
                        string respuesta = System.Text.Json.JsonSerializer.Serialize(mCargadorResultadosModel.ListaIdsResultado);
                        return Content(respuesta);
                    }
                    else
                    {
                        #region Cargamos los resultados
                        List<Guid> listaComunidadesID = new List<Guid>();
                        List<Guid> listaRecursosID = new List<Guid>();
                        List<Guid> listaIdentidadesID = new List<Guid>();
                        List<Guid> listaGruposID = new List<Guid>();
                        List<Guid> listaMensajesID = new List<Guid>();
                        List<Guid> listaComentariosID = new List<Guid>();
                        List<Guid> listaInvitacionesID = new List<Guid>();
                        List<Guid> listaPersonasContactoID = new List<Guid>();
                        List<Guid> listaOrganizacionesContactoID = new List<Guid>();
                        List<Guid> listaGruposContactoID = new List<Guid>();
                        List<Guid> listaPaginasCMSID = new List<Guid>();

                        foreach (string idResultado in mCargadorResultadosModel.ListaIdsResultado.Keys)
                        {
                            switch (mCargadorResultadosModel.ListaIdsResultado[idResultado])
                            {
                                case TiposResultadosMetaBuscador.Comunidad:
                                    listaComunidadesID.Add(new Guid(idResultado));
                                    break;
                                case TiposResultadosMetaBuscador.Documento:
                                case TiposResultadosMetaBuscador.DocumentoBRPrivada:
                                case TiposResultadosMetaBuscador.DocumentoBRPersonal:
                                case TiposResultadosMetaBuscador.Pregunta:
                                case TiposResultadosMetaBuscador.Debate:
                                case TiposResultadosMetaBuscador.Encuesta:
                                    listaRecursosID.Add(new Guid(idResultado));
                                    break;
                                case TiposResultadosMetaBuscador.IdentidadPersona:
                                case TiposResultadosMetaBuscador.IdentidadOrganizacion:
                                    listaIdentidadesID.Add(new Guid(idResultado));
                                    break;
                                case TiposResultadosMetaBuscador.Grupo:
                                    listaGruposID.Add(new Guid(idResultado));
                                    break;
                                case TiposResultadosMetaBuscador.Mensaje:
                                    listaMensajesID.Add(new Guid(idResultado));
                                    break;
                                case TiposResultadosMetaBuscador.Comentario:
                                    listaComentariosID.Add(new Guid(idResultado));
                                    break;
                                case TiposResultadosMetaBuscador.Invitacion:
                                    listaInvitacionesID.Add(new Guid(idResultado));
                                    break;
                                case TiposResultadosMetaBuscador.PerContacto:
                                    listaPersonasContactoID.Add(new Guid(idResultado));
                                    break;
                                case TiposResultadosMetaBuscador.OrgContacto:
                                    listaOrganizacionesContactoID.Add(new Guid(idResultado));
                                    break;
                                case TiposResultadosMetaBuscador.GrupoContacto:
                                    listaGruposContactoID.Add(new Guid(idResultado));
                                    break;
                                case TiposResultadosMetaBuscador.PaginaCMS:
                                    listaPaginasCMSID.Add(new Guid(idResultado));
                                    break;
                            }
                        }

                        string urlBaseResultados = "";
                        if (mCargadorResultadosModel.Proyecto.Clave.Equals(ProyectoAD.MetaProyecto))
                        {
                            if (proyectoVirtualID.Equals(ProyectoAD.MetaProyecto))
                            {
                                urlBaseResultados = BaseURL;
                            }
                            else
                            {
                                ProyectoCL proyCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                                urlBaseResultados = proyCL.ObtenerURLPropiaProyecto(proyectoVirtualID, mControladorBase.IdiomaUsuario);
                            }
                        }
                        else
                        {
                            urlBaseResultados = mCargadorResultadosModel.Proyecto.UrlPropia(mControladorBase.IdiomaUsuario);
                        }
                        urlBaseResultados = urlBaseResultados.TrimEnd('/');

                        if (UtilIdiomas.LanguageCode != "es")
                        {
                            urlBaseResultados += "/" + UtilIdiomas.LanguageCode;
                        }

                        string baseUrlBusqueda = "";

                        if (mCargadorResultadosModel.FilaPestanyaActual != null)
                        {
                            string url = string.Empty;
                            if (string.IsNullOrEmpty(mCargadorResultadosModel.FilaPestanyaActual.Ruta))
                            {
                                switch (mCargadorResultadosModel.FilaPestanyaActual.TipoPestanya)
                                {
                                    case (short)TipoPestanyaMenu.Recursos:
                                        url = UtilIdiomas.GetText("URLSEM", "RECURSOS");
                                        break;
                                    case (short)TipoPestanyaMenu.Preguntas:
                                        url = UtilIdiomas.GetText("URLSEM", "PREGUNTAS");
                                        break;
                                    case (short)TipoPestanyaMenu.Debates:
                                        url = UtilIdiomas.GetText("URLSEM", "DEBATES");
                                        break;
                                    case (short)TipoPestanyaMenu.Encuestas:
                                        url = UtilIdiomas.GetText("URLSEM", "ENCUESTAS");
                                        break;
                                    case (short)TipoPestanyaMenu.PersonasYOrganizaciones:
                                        url = UtilIdiomas.GetText("URLSEM", "PERSONASYORGANIZACIONES");
                                        break;
                                    case (short)TipoPestanyaMenu.AcercaDe:
                                        url = UtilIdiomas.GetText("URLSEM", "ACERCADE");
                                        break;
                                    case (short)TipoPestanyaMenu.BusquedaAvanzada:
                                        url = UtilIdiomas.GetText("URLSEM", "BUSQUEDAAVANZADA");
                                        break;
                                }
                            }
                            else
                            {
                                url = UtilCadenas.ObtenerTextoDeIdioma(mCargadorResultadosModel.FilaPestanyaActual.Ruta, UtilIdiomas.LanguageCode, null);
                            }

                            if (mCargadorResultadosModel.Proyecto.Clave != ProyectoAD.MetaProyecto)
                            {
                                if (!mCargadorResultadosModel.ParametroProyecto.ContainsKey(ParametroAD.ProyectoSinNombreCortoEnURL) || !mCargadorResultadosModel.ParametroProyecto[ParametroAD.ProyectoSinNombreCortoEnURL].Equals("1"))
                                {
                                    baseUrlBusqueda = "/" + UtilIdiomas.GetText("URLSEM", "COMUNIDAD") + "/" + mCargadorResultadosModel.Proyecto.NombreCorto + "/" + url;
                                }
                                else
                                {
                                    baseUrlBusqueda = "/" + url;
                                }
                            }
                            else if (mCargadorResultadosModel.IdentidadActual.TrabajaConOrganizacion)
                            {
                                baseUrlBusqueda = "/" + UtilIdiomas.GetText("URLSEM", "IDENTIDAD") + "/" + mCargadorResultadosModel.IdentidadActual.PerfilUsuario.NombreCortoOrg + "/" + url;
                            }
                            else
                            {
                                baseUrlBusqueda = "/" + url;
                            }
                            baseUrlBusqueda = urlBaseResultados + baseUrlBusqueda;
                        }
                        else if (!string.IsNullOrEmpty(pUrlPaginaBusqueda) && !mCargadorResultadosModel.Proyecto.Clave.Equals(ProyectoAD.MetaProyecto))
                        {
                            if (pUrlPaginaBusqueda.Contains(UtilIdiomas.GetText("URLSEM", "COMUNIDAD") + "/" + mCargadorResultadosModel.Proyecto.NombreCorto + "/" + UtilIdiomas.GetText("URLSEM", "BUSQUEDAAVANZADA")))
                            {
                                baseUrlBusqueda = urlBaseResultados + "/" + UtilIdiomas.GetText("URLSEM", "COMUNIDAD") + "/" + mCargadorResultadosModel.Proyecto.NombreCorto + "/" + UtilIdiomas.GetText("URLSEM", "BUSQUEDAAVANZADA");
                            }
                            else if (pUrlPaginaBusqueda.Contains(UtilIdiomas.GetText("URLSEM", "BUSQUEDAAVANZADA")))
                            {
                                baseUrlBusqueda = urlBaseResultados + "/" + UtilIdiomas.GetText("URLSEM", "BUSQUEDAAVANZADA");
                            }
                        }

                        if (string.IsNullOrEmpty(baseUrlBusqueda))
                        {
                            if (!string.IsNullOrEmpty(pUrlPaginaBusqueda))
                            {
                                baseUrlBusqueda = pUrlPaginaBusqueda;
                            }
                            else
                            {
                                baseUrlBusqueda = new Uri(UrlPaginaActual).PathAndQuery;
                                baseUrlBusqueda = baseUrlBusqueda.Substring(IdiomaPagina.Length);
                            }
                            if (baseUrlBusqueda.Contains("?"))
                            {
                                baseUrlBusqueda = baseUrlBusqueda.Substring(0, baseUrlBusqueda.IndexOf("?"));
                            }

                            if (mCargadorResultadosModel.Proyecto.Clave != ProyectoAD.MetaProyecto && baseUrlBusqueda.StartsWith("/" + UtilIdiomas.GetText("URLSEM", "COMUNIDAD") + "/" + mCargadorResultadosModel.Proyecto.NombreCorto + "/" + UtilIdiomas.GetText("URLSEM", "CATEGORIA")))
                            {
                                baseUrlBusqueda = "/" + UtilIdiomas.GetText("URLSEM", "COMUNIDAD") + "/" + mCargadorResultadosModel.Proyecto.NombreCorto + "/" + UtilIdiomas.GetText("URLSEM", "BUSQUEDAAVANZADA");
                            }
                            else if (mCargadorResultadosModel.Proyecto.Clave != ProyectoAD.MetaProyecto && baseUrlBusqueda.StartsWith("/" + UtilIdiomas.GetText("URLSEM", "CATEGORIA")))
                            {
                                baseUrlBusqueda = "/" + UtilIdiomas.GetText("URLSEM", "BUSQUEDAAVANZADA");
                            }
                            else
                            {
                                bool paginaEncontrada = false;
                                foreach (ProyectoPestanyaMenu filaPestanya in mCargadorResultadosModel.PestanyasProyecto.ListaProyectoPestanyaMenu)
                                {
                                    string url = string.Empty;

                                    if (string.IsNullOrEmpty(filaPestanya.Ruta))
                                    {
                                        switch (filaPestanya.TipoPestanya)
                                        {
                                            case (short)TipoPestanyaMenu.Recursos:
                                                url = UtilIdiomas.GetText("URLSEM", "RECURSOS");
                                                break;
                                            case (short)TipoPestanyaMenu.Preguntas:
                                                url = UtilIdiomas.GetText("URLSEM", "PREGUNTAS");
                                                break;
                                            case (short)TipoPestanyaMenu.Debates:
                                                url = UtilIdiomas.GetText("URLSEM", "DEBATES");
                                                break;
                                            case (short)TipoPestanyaMenu.Encuestas:
                                                url = UtilIdiomas.GetText("URLSEM", "ENCUESTAS");
                                                break;
                                            case (short)TipoPestanyaMenu.PersonasYOrganizaciones:
                                                url = UtilIdiomas.GetText("URLSEM", "PERSONASYORGANIZACIONES");
                                                break;
                                            case (short)TipoPestanyaMenu.AcercaDe:
                                                url = UtilIdiomas.GetText("URLSEM", "ACERCADE");
                                                break;
                                            case (short)TipoPestanyaMenu.BusquedaAvanzada:
                                                url = UtilIdiomas.GetText("URLSEM", "BUSQUEDAAVANZADA");
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        url = UtilCadenas.ObtenerTextoDeIdioma(filaPestanya.Ruta, UtilIdiomas.LanguageCode, null);
                                    }

                                    if (!string.IsNullOrEmpty(url))
                                    {
                                        if (mCargadorResultadosModel.Proyecto.Clave != ProyectoAD.MetaProyecto && baseUrlBusqueda.StartsWith("/" + UtilIdiomas.GetText("URLSEM", "COMUNIDAD") + "/" + mCargadorResultadosModel.Proyecto.NombreCorto + "/" + url))
                                        {
                                            baseUrlBusqueda = "/" + UtilIdiomas.GetText("URLSEM", "COMUNIDAD") + "/" + mCargadorResultadosModel.Proyecto.NombreCorto + "/" + url;
                                            paginaEncontrada = true;
                                            break;
                                        }
                                        else if (mCargadorResultadosModel.IdentidadActual.TrabajaConOrganizacion && baseUrlBusqueda.StartsWith("/" + UtilIdiomas.GetText("URLSEM", "IDENTIDAD") + "/" + mCargadorResultadosModel.IdentidadActual.PerfilUsuario.NombreCortoOrg + "/" + url))
                                        {
                                            baseUrlBusqueda = "/" + UtilIdiomas.GetText("URLSEM", "IDENTIDAD") + "/" + mCargadorResultadosModel.IdentidadActual.PerfilUsuario.NombreCortoOrg + "/" + url;
                                            paginaEncontrada = true;
                                            break;
                                        }
                                        else if (baseUrlBusqueda.StartsWith("/" + url))
                                        {
                                            baseUrlBusqueda = "/" + url;
                                            paginaEncontrada = true;
                                            break;
                                        }
                                    }
                                }
                                if (!paginaEncontrada)
                                {
                                    string[] pestanyas = { UtilIdiomas.GetText("URLSEM", "MISCONTRIBUCIONES"), UtilIdiomas.GetText("URLSEM", "BORRADORES"), UtilIdiomas.GetText("URLSEM", "COMUNIDADES"), UtilIdiomas.GetText("URLSEM", "MISRECURSOS") };
                                    foreach (string pest in pestanyas)
                                    {
                                        if (!string.IsNullOrEmpty(pest))
                                        {
                                            if (mCargadorResultadosModel.Proyecto.Clave != ProyectoAD.MetaProyecto && baseUrlBusqueda.StartsWith("/" + UtilIdiomas.GetText("URLSEM", "COMUNIDAD") + "/" + mCargadorResultadosModel.Proyecto.NombreCorto + "/" + pest))
                                            {
                                                baseUrlBusqueda = "/" + UtilIdiomas.GetText("URLSEM", "COMUNIDAD") + "/" + mCargadorResultadosModel.Proyecto.NombreCorto + "/" + pest;
                                                paginaEncontrada = true;
                                                break;
                                            }
                                            else if (mCargadorResultadosModel.IdentidadActual.TrabajaConOrganizacion && baseUrlBusqueda.StartsWith("/" + UtilIdiomas.GetText("URLSEM", "IDENTIDAD") + "/" + mCargadorResultadosModel.IdentidadActual.PerfilUsuario.NombreCortoOrg + "/" + pest))
                                            {
                                                baseUrlBusqueda = "/" + UtilIdiomas.GetText("URLSEM", "IDENTIDAD") + "/" + mCargadorResultadosModel.IdentidadActual.PerfilUsuario.NombreCortoOrg + "/" + pest;
                                                break;
                                            }
                                            else if (baseUrlBusqueda.StartsWith("/" + pest))
                                            {
                                                baseUrlBusqueda = "/" + pest;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            baseUrlBusqueda = urlBaseResultados + baseUrlBusqueda;
                        }

                        ControladorProyectoMVC controladorMVC = new ControladorProyectoMVC(UtilIdiomas, BaseURL, BaseURLsContent, BaseURLStatic, mCargadorResultadosModel.Proyecto, mCargadorResultadosModel.ProyectoOrigenID, mCargadorResultadosModel.FilaParametroGeneral, mCargadorResultadosModel.IdentidadActual, mCargadorResultadosModel.EsBot, mLoggingService, mEntityContext, mConfigService, mHttpContextAccessor, mRedisCacheWrapper, mVirtuosoAD, mGnossCache, mEntityContextBASE, mServicesUtilVirtuosoAndReplication);

                        Dictionary<Guid, CommunityModel> listaComunidadesModel = new Dictionary<Guid, CommunityModel>();
                        if (listaComunidadesID.Count > 0)
                        {
                            listaComunidadesModel = controladorMVC.ObtenerComunidadesPorID(listaComunidadesID, baseUrlBusqueda);
                        }

                        Dictionary<Guid, ResourceModel> listaRecursosModel = new Dictionary<Guid, ResourceModel>();
                        if (listaRecursosID.Count > 0)
                        {
                            EspacioPersonal espacioPersonal = EspacioPersonal.No;

                            if (pTipoBusqueda == (short)TipoBusqueda.EditarRecursosPerfil)
                            {
                                espacioPersonal = EspacioPersonal.Usuario;
                            }

                            listaRecursosModel = controladorMVC.ObtenerRecursosPorID(listaRecursosID, baseUrlBusqueda, espacioPersonal, mCargadorResultadosModel.PestanyaActualID, !mCargadorResultadosModel.SinDatosExtra, true, obtenerDatosExtraIdentidades);

                            CargarDatosExtraRecursos(listaRecursosModel);

                            UtilServicioResultados.ProcesarFichasRecursoParaPresentacion(listaRecursosModel);
                            AjustarModeloRecursos(listaRecursosModel, pTipoBusqueda);
                        }

                        Dictionary<Guid, ProfileModel> listaIdentidadesModel = new Dictionary<Guid, ProfileModel>();
                        if (listaIdentidadesID.Count > 0)
                        {
                            listaIdentidadesModel = controladorMVC.ObtenerIdentidadesPorID(listaIdentidadesID);

                            controladorMVC.ObtenerInfoExtraIdentidadesPorID(listaIdentidadesModel);
                        }

                        Dictionary<Guid, GroupCardModel> listaGruposModel = new Dictionary<Guid, GroupCardModel>();
                        if (listaGruposID.Count > 0)
                        {
                            listaGruposModel = controladorMVC.ObtenerGruposPorID(listaGruposID);
                        }

                        Dictionary<Guid, MessageModel> listaMensajesModel = new Dictionary<Guid, MessageModel>();
                        if (listaMensajesID.Count > 0)
                        {
                            listaMensajesModel = controladorMVC.ObtenerMensajesPorID(listaMensajesID, pParametros, mCargadorResultadosModel.IdentidadActual);
                        }

                        Dictionary<Guid, CommentSearchModel> listaComentariosModel = new Dictionary<Guid, CommentSearchModel>();
                        if (listaComentariosID.Count > 0)
                        {
                            Guid grafoID = Guid.Empty;
                            Guid.TryParse(pGrafo, out grafoID);

                            listaComentariosModel = controladorMVC.ObtenerComentariosPorID(listaComentariosID, grafoID);
                        }

                        Dictionary<Guid, InvitationModel> listaInvitacionesModel = new Dictionary<Guid, InvitationModel>();
                        if (listaInvitacionesID.Count > 0)
                        {
                            listaInvitacionesModel = controladorMVC.ObtenerInvitacionesPorID(listaInvitacionesID);
                        }

                        Dictionary<Guid, ContactModel> listaContactosModel = new Dictionary<Guid, ContactModel>();
                        if (listaPersonasContactoID.Count > 0 || listaOrganizacionesContactoID.Count > 0 || listaGruposContactoID.Count > 0)
                        {
                            listaContactosModel = controladorMVC.ObtenerContactosPorID(listaPersonasContactoID, listaOrganizacionesContactoID, listaGruposContactoID);
                        }

                        Dictionary<Guid, PaginaCMSModel> listaPaginasCMSModel = new Dictionary<Guid, PaginaCMSModel>();
                        if (listaPaginasCMSID.Count > 0)
                        {
                            // TODO: Alberto, crear un nuevo modelo o reutiliozar el de contactos?
                            listaPaginasCMSModel = controladorMVC.ObtenerPaginasCMSPorID(listaPaginasCMSID, proyectoID);
                        }

                        #endregion

                        #region Construimos la vista

                        ViewBag.UtilIdiomas = UtilIdiomas;
                        ViewBag.EsCatalogo = mCargadorResultadosModel.Proyecto.EsCatalogo;
                        ViewBag.MostrarPersonasEnCatalogo = mCargadorResultadosModel.FilaParametroGeneral.MostrarPersonasEnCatalogo;
                        ViewBag.ProyectoID = proyectoID;
                        ViewBag.BaseUrlContent = BaseURLsContent[0];
                        ViewBag.BaseUrlStatic = BaseURLStatic;
                        ViewBag.BaseUrlPersonalizacion = mControladorBase.BaseURLPersonalizacion;
                        ViewBag.BaseUrlPersonalizacionEcosistema = mControladorBase.BaseURLPersonalizacionEcosistema;
                        ViewBag.GeneradorURLs = mControladorBase.UrlsSemanticas;
                        List<ObjetoBuscadorModel> ListaResultados = new List<ObjetoBuscadorModel>();

                        foreach (string idResultado in mCargadorResultadosModel.ListaIdsResultado.Keys)
                        {
                            Guid resultadoID = new Guid(idResultado);
                            try
                            {
                                switch (mCargadorResultadosModel.ListaIdsResultado[idResultado])
                                {
                                    case TiposResultadosMetaBuscador.Comunidad:
                                        if (listaComunidadesModel.ContainsKey(resultadoID))
                                        {
                                            CommunityModel objetoBuscador = listaComunidadesModel[resultadoID];
                                            if (objetoBuscador.Name.Contains("|||"))
                                            {
                                                List<string> nombresIdioma = objetoBuscador.Name.Split(new string[] { "|||" }, StringSplitOptions.None).ToList();
                                                Dictionary<string, string> nombreIdiomaDictionary = new Dictionary<string, string>();
                                                foreach (string nombreCode in nombresIdioma)
                                                {
                                                    if (nombreCode.Contains("@"))
                                                    {
                                                        string[] idiomas = nombreCode.Split('@');
                                                        nombreIdiomaDictionary.Add(idiomas[1], idiomas[0]);
                                                    }

                                                }

                                                if (nombreIdiomaDictionary.ContainsKey(UtilIdiomas.LanguageCode))
                                                {
                                                    objetoBuscador.Name = nombreIdiomaDictionary[UtilIdiomas.LanguageCode];
                                                }
                                                else
                                                {
                                                    objetoBuscador.Name = nombreIdiomaDictionary.Values.First();
                                                }
                                            }
                                            ListaResultados.Add(objetoBuscador);

                                        }
                                        break;
                                    case TiposResultadosMetaBuscador.Documento:
                                    case TiposResultadosMetaBuscador.DocumentoBRPrivada:
                                    case TiposResultadosMetaBuscador.DocumentoBRPersonal:
                                    case TiposResultadosMetaBuscador.Pregunta:
                                    case TiposResultadosMetaBuscador.Debate:
                                    case TiposResultadosMetaBuscador.Encuesta:
                                        if (listaRecursosModel.ContainsKey(resultadoID))
                                        {
                                            ListaResultados.Add(listaRecursosModel[resultadoID]);
                                        }
                                        break;
                                    case TiposResultadosMetaBuscador.IdentidadPersona:
                                    case TiposResultadosMetaBuscador.IdentidadOrganizacion:
                                        if (listaIdentidadesModel.ContainsKey(resultadoID))
                                        {
                                            ListaResultados.Add(listaIdentidadesModel[resultadoID]);
                                        }
                                        break;
                                    case TiposResultadosMetaBuscador.Grupo:
                                        if (listaGruposModel.ContainsKey(resultadoID))
                                        {
                                            ListaResultados.Add(listaGruposModel[resultadoID]);
                                        }
                                        break;
                                    case TiposResultadosMetaBuscador.Mensaje:
                                        if (listaMensajesModel.ContainsKey(resultadoID))
                                        {
                                            ListaResultados.Add(listaMensajesModel[resultadoID]);
                                        }
                                        break;
                                    case TiposResultadosMetaBuscador.Comentario:
                                        if (listaComentariosModel.ContainsKey(resultadoID))
                                        {
                                            ListaResultados.Add(listaComentariosModel[resultadoID]);
                                        }
                                        break;
                                    case TiposResultadosMetaBuscador.Invitacion:
                                        if (listaInvitacionesModel.ContainsKey(resultadoID))
                                        {
                                            ListaResultados.Add(listaInvitacionesModel[resultadoID]);
                                        }
                                        break;
                                    case TiposResultadosMetaBuscador.PerContacto:
                                    case TiposResultadosMetaBuscador.OrgContacto:
                                    case TiposResultadosMetaBuscador.GrupoContacto:
                                        if (listaContactosModel.ContainsKey(resultadoID))
                                        {
                                            ListaResultados.Add(listaContactosModel[resultadoID]);
                                        }
                                        break;
                                    case TiposResultadosMetaBuscador.PaginaCMS:
                                        if (listaPaginasCMSModel.ContainsKey(resultadoID))
                                        {
                                            ListaResultados.Add(listaPaginasCMSModel[resultadoID]);
                                        }
                                        break;
                                }
                            }
                            catch
                            {
                            }
                        }


                        ResultadoModel resultadoModel = new ResultadoModel();
                        resultadoModel.NumeroResultadosTotal = mCargadorResultadosModel.NumeroResultados;
                        resultadoModel.NumeroResultadosPagina = mCargadorResultadosModel.FilasPorPagina;
                        resultadoModel.NumeroPaginaActual = mCargadorResultadosModel.PaginaActual;
                        resultadoModel.UrlBusqueda = mCargadorResultadosModel.UrlNavegador;
                        resultadoModel.ListaResultados = ListaResultados;
                        // Asignar propiedad para saber si la petición se ha realizado desde Administración o no
                        resultadoModel.AdministradorVeTodasPersonas = pAdministradorVeTodasPersonas;

                        //foreach (ObjetoBuscadorModel)
                        //{
                        //    if(UtilIdiomas.LanguageCode)
                        //}
                        resultadoModel.TipoBusqueda = (ResultadoModel.TiposBusquedaMVC)pTipoBusqueda;
                        if (resultadoModel.ListaResultados == null || resultadoModel.ListaResultados.Count == 0)
                        {
                            resultadoModel.TextoSinResultados = obtenerTextoBusquedaSinResultados();
                        }

                        ViewData.Model = resultadoModel;

                        CargarPersonalizacion(proyectoVirtualID, controladorMVC);

                        bool jsonRequest = Request.Headers.ContainsKey("Accept") && Request.Headers["Accept"].Equals("application/json");
                        if (string.IsNullOrEmpty(funcionCallBack) || jsonRequest)
                        {
                            if ((pJson.HasValue && pJson.Value) || jsonRequest)
                            {
                                JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
                                {
                                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                                    TypeNameHandling = TypeNameHandling.All
                                };
                                string respuesta = JsonConvert.SerializeObject(resultadoModel, jsonSerializerSettings);

                                if (Request.Headers["User-Agent"].Contains("GnossInternalRequest"))
                                {
                                    using (MemoryStream input = new MemoryStream())
                                    {
                                        BinaryFormatter bformatter = new BinaryFormatter();
                                        bformatter.Serialize(input, resultadoModel);
                                        input.Seek(0, SeekOrigin.Begin);

                                        using (MemoryStream output = new MemoryStream())
                                        using (DeflateStream deflateStream = new DeflateStream(output, CompressionMode.Compress))
                                        {
                                            input.CopyTo(deflateStream);
                                            deflateStream.Close();

                                            respuesta = Convert.ToBase64String(output.ToArray());
                                        }
                                    }
                                    respuesta = SerializeViewData(respuesta);
                                }
                                return Content(respuesta);
                            }
                            else
                            {
                                //return View("CargarResultados");
                                string resultado = "";
                                using (StringWriter sw = new StringWriter())
                                {
                                    try
                                    {
                                        ViewEngineResult viewResult = mViewEngine.FindView(ControllerContext, ObtenerNombreVista("CargarResultados"), false);

                                        if (viewResult.View == null) throw new Exception("View not found: CargarResultados");
                                        ViewContext viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw, new HtmlHelperOptions());
                                        Task renderTask = viewResult.View.RenderAsync(viewContext);
                                        renderTask.Wait();
                                        if (renderTask.Exception != null)
                                        {
                                            mLoggingService.GuardarLogError(renderTask.Exception);
                                        }
                                        resultado = sw.GetStringBuilder().ToString();
                                    }
                                    catch (Exception ex)
                                    {
                                        mLoggingService.GuardarLogError(ex);
                                    }
                                }

                                KeyValuePair<int, string> resultadoPeticion = new KeyValuePair<int, string>(resultadoModel.NumeroResultadosTotal, resultado);

                                //Devuelvo la respuesta en el response de la petición
                                return Content(System.Text.Json.JsonSerializer.Serialize(resultadoPeticion));
                            }
                        }
                        else
                        {
                            string resultado = "";
                            using (StringWriter sw = new StringWriter())
                            {
                                try
                                {
                                    ViewEngineResult viewResult = mViewEngine.FindView(ControllerContext, ObtenerNombreVista("CargarResultados"), false);
                                    if (viewResult.View == null) throw new Exception("View not found: CargarResultados");
                                    ViewContext viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw, new HtmlHelperOptions());
                                    Task renderTask = viewResult.View.RenderAsync(viewContext);
                                    renderTask.Wait();
                                    if (renderTask.Exception != null)
                                    {
                                        mLoggingService.GuardarLogError(renderTask.Exception);
                                    }

                                    resultado = sw.GetStringBuilder().ToString();
                                }
                                catch (Exception ex)
                                {
                                    mLoggingService.GuardarLogError(ex);
                                }
                            }

                            //Devuelvo la respuesta en el response de la petición
                            HttpContext.Response.ContentType = "text/plain";
                            HttpContext.Response.WriteAsync(funcionCallBack + "({\"d\":" + System.Text.Json.JsonSerializer.Serialize(resultado) + "});");
                        }
                        #endregion
                    }
                }
                else if (tipoResultadoBusqueda == TipoResultadoBusqueda.PaginaMayorMil)
                {
                    ////TODO ALVARO
                    //if (mCargadorResultadosModel.NumeroParteResultados == 1)
                    //{
                    //    ObtenerHtmlPerdidos(mPanelTotal);
                    //    StringWriter sr = new StringWriter();
                    //    HtmlTextWriter writer = new HtmlTextWriter(sr);
                    //    mPanelTotal.RenderControl(writer);
                    //    resultado += writer.InnerWriter;
                    //}
                    //return resultado;      
                }
                else if (tipoResultadoBusqueda == TipoResultadoBusqueda.FiltrosNoPermitidosAusuario)
                {
                    ////TODO ALVARO
                    ////Si el usuario no está en el proyecto, no puede filtrar por estos filtros
                    //if (mCargadorResultadosModel.NumeroParteResultados == 1)
                    //{
                    //    ObtenerHtmlSinResultados(mPanelTotal);
                    //    StringWriter sr = new StringWriter();
                    //    HtmlTextWriter writer = new HtmlTextWriter(sr);
                    //    mPanelTotal.RenderControl(writer);
                    //    resultado += writer.InnerWriter;
                    //}
                    //return resultado;
                }
            }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                string url = HttpContext.Request.Path;
                if (!url.Contains("?"))
                {
                    url += "?" + HttpContext.Request.QueryString.ToString();
                }

                mUtilServicios.EnviarErrorYGuardarLog("Error: " + ex.Message + "\r\nPila: " + ex.StackTrace + "\r\nLlamada: " + url, "errorBots", mCargadorResultadosModel.EsBot);
            }
            return new EmptyResult();
        }
        [NonAction]
        private string obtenerTextoBusquedaSinResultados()
        {
            string textoSinResultados = "";

            if (mCargadorResultadosModel.TipoBusqueda == TipoBusqueda.Comentarios)
            {
                textoSinResultados = mUtilIdiomas.GetText("METABUSCADOR", "NORESULTADOSCOMENT");
            }
            else if (mCargadorResultadosModel.TipoBusqueda == TipoBusqueda.Invitaciones)
            {
                textoSinResultados = mUtilIdiomas.GetText("METABUSCADOR", "NORESULTADOSINVT");
            }
            else if (mCargadorResultadosModel.TipoBusqueda == TipoBusqueda.Notificaciones)
            {
                textoSinResultados = mUtilIdiomas.GetText("METABUSCADOR", "NORESULTADOSNOT");
            }
            else if (mCargadorResultadosModel.TipoBusqueda == TipoBusqueda.Suscripciones)
            {
                textoSinResultados = mUtilIdiomas.GetText("METABUSCADOR", "NORESULTADOSSUSCRIP");
            }
            else
            {
                bool montarHtmlDefecto = true;
                if (mCargadorResultadosModel.FilaPestanyaActual != null && mCargadorResultadosModel.FilaPestanyaActual.ProyectoPestanyaBusqueda != null && mCargadorResultadosModel.FilaPestanyaActual.ProyectoPestanyaBusqueda.TextoBusquedaSinResultados != null)
                {
                    textoSinResultados = UtilCadenas.ObtenerTextoDeIdioma(mCargadorResultadosModel.FilaPestanyaActual.ProyectoPestanyaBusqueda.TextoBusquedaSinResultados, mUtilIdiomas.LanguageCode, mCargadorResultadosModel.FilaParametroGeneral.IdiomaDefecto);
                    montarHtmlDefecto = false;
                }

                if (montarHtmlDefecto)
                {
                    textoSinResultados = mUtilIdiomas.GetText("METABUSCADOR", "NORESULTADOSBUSQUEDA");
                }
            }
            return textoSinResultados;
        }
        [NonAction]
        private string SerializeViewData(string json)
        {
            ViewBag.ControladorProyectoMVC = null;
            UtilIdiomasSerializable aux = ViewBag.UtilIdiomas.GetUtilIdiomas();
            ViewBag.UtilIdiomas = aux;

            JsonSerializerSettings jsonSerializerSettingsVB = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Full
            };
            Dictionary<string, object> dic = ViewData.Where(k => !k.Key.Equals("LoggingService")).ToDictionary(k => k.Key, v => v.Value);
            string jsonViewData = JsonConvert.SerializeObject(dic, jsonSerializerSettingsVB);

            return json + "{ComienzoJsonViewData}" + jsonViewData;
        }
        [NonAction]
        private void CargarDatosExtraRecursos(Dictionary<Guid, ResourceModel> listaRecursosModel)
        {
            if (mCargadorResultadosModel.ParametroProyecto.ContainsKey(ParametroAD.CargarEditoresLectoresEnBusqueda) && mCargadorResultadosModel.ParametroProyecto[ParametroAD.CargarEditoresLectoresEnBusqueda].Equals("1"))
            {
                List<Guid> recursosSinCargarPorCompleto = listaRecursosModel.Values.Where(resource => !resource.FullyLoaded).Select(resource => resource.Key).ToList();
                if (recursosSinCargarPorCompleto.Count > 0)
                {
                    DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                    GestorDocumental gestorDocumental = new GestorDocumental(docCN.ObtenerDocumentosPorID(recursosSinCargarPorCompleto), mLoggingService, mEntityContext);

                    ControladorDocumentoMVC controladorDocumentoMVC = new ControladorDocumentoMVC(mLoggingService, mConfigService, mEntityContext, mRedisCacheWrapper, mGnossCache, mVirtuosoAD, mHttpContextAccessor, mServicesUtilVirtuosoAndReplication);

                    foreach (Guid documentoID in recursosSinCargarPorCompleto)
                    {
                        if (!listaRecursosModel[documentoID].FullyLoaded)
                        {
                            controladorDocumentoMVC.CargarEditoresLectores(listaRecursosModel[documentoID], gestorDocumental, gestorDocumental.ListaDocumentos[documentoID], mCargadorResultadosModel.IdentidadActual, !mCargadorResultadosModel.EstaEnProyecto, true, mUtilIdiomas, BaseURLIdioma, mCargadorResultadosModel.Proyecto, mControladorBase.ProyectoVirtual);
                        }
                    }
                }
            }

            ProyectoCL proyectoCL = new ProyectoCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
            mCargadorResultadosModel.Proyecto.GestorProyectos.DataWrapperProyectos.ListaNivelCertificacion = mCargadorResultadosModel.Proyecto.GestorProyectos.DataWrapperProyectos.ListaNivelCertificacion.Union(proyectoCL.ObtenerNivelesCertificacionRecursosProyecto(mCargadorResultadosModel.ProyectoSeleccionado).ListaNivelCertificacion).ToList();
            if (mCargadorResultadosModel.Proyecto.GestorProyectos.DataWrapperProyectos.ListaNivelCertificacion.Count > 0)
            {
                DocumentacionCN docCN = new DocumentacionCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);

                List<Guid> recursosSinCargarPorCompleto = listaRecursosModel.Values.Select(resource => resource.Key).ToList();
                Dictionary<Guid, Dictionary<Guid, string>> ListaNivelesCertificacion = docCN.ObtenerNivelCertificacionDeDocumentos(recursosSinCargarPorCompleto, mCargadorResultadosModel.ProyectoSeleccionado);
                foreach (KeyValuePair<Guid, Dictionary<Guid, string>> val in ListaNivelesCertificacion)
                {
                    listaRecursosModel[val.Key].Certification = val.Value.First();
                }

            }
        }
        [HttpGet, HttpPost]
        [Route("CargarResultadosContexto")]
        public ActionResult CargarResultadosContexto([FromForm] string pProyectoID, [FromForm] string pParametros, [FromForm] bool pPrimeraCarga, [FromForm] string pLanguageCode, short pTipoBusqueda, [FromForm] int pNumRecursosPagina, [FromForm] string pGrafo, [FromForm] string pUrlPaginaBusqueda, [FromForm] string pFiltroContexto, [FromForm] bool pEsBot, [FromForm] bool pMostrarEnlaceOriginal, [FromForm] string pNamespacesExtra, [FromForm] string pListaItemsBusqueda, [FromForm] string pResultadosEliminar, [FromForm] bool pNuevaPestanya, [FromForm] string pParametrosAdicionales, [FromForm] string pIdentidadID, [FromForm] bool pEsUsuarioInvitado)
        {
            try
            {
                mLoggingService.AgregarEntrada("Empieza carga de contextos");
                TipoFichaResultados tipoFicha = TipoFichaResultados.Contexto;

                #region Obtenemos parámetros

                pProyectoID = pProyectoID.Replace("\"", "");
                pGrafo = pGrafo.Replace("\"", "");
                if (!string.IsNullOrEmpty(pParametrosAdicionales))
                {
                    pParametrosAdicionales = pParametrosAdicionales.Replace("\"", "");
                }
                else
                {
                    pParametrosAdicionales = string.Empty;
                }
                
                pParametros = pParametros.Replace("\"", "");
                pLanguageCode = pLanguageCode.Replace("\"", "");

                if (!string.IsNullOrEmpty(pIdentidadID))
                {
                    pIdentidadID = pIdentidadID.Replace("\"", "");
                }

                if (!string.IsNullOrEmpty(pFiltroContexto) && pFiltroContexto.Length > 2 && pFiltroContexto.StartsWith("\"") && pFiltroContexto.EndsWith("\""))
                {
                    pFiltroContexto = pFiltroContexto.Substring(1, pFiltroContexto.Length - 2);
                }

                Guid identidadID, proyectoID;
                Guid.TryParse(pProyectoID, out proyectoID);

                if (!string.IsNullOrEmpty(pIdentidadID) && pIdentidadID != Guid.Empty.ToString())
                {
                    Guid.TryParse(pIdentidadID, out identidadID);
                }
                else
                {
                    identidadID = UsuarioAD.Invitado;
                    pEsUsuarioInvitado = false;
                }

                mCargadorResultadosModel.EsContexto = true;
                mCargadorResultadosModel.ProyectoSeleccionado = proyectoID;
                mCargadorResultadosModel.EsBot = pEsBot;
                mCargadorResultadosModel.LanguageCode = pLanguageCode;
                mCargadorResultadosModel.EsMyGnoss = proyectoID == ProyectoAD.MetaProyecto;

                if (!string.IsNullOrEmpty(pNamespacesExtra))
                {
                    mCargadorResultadosModel.NamespacesExtra = " " + pNamespacesExtra + " ";
                }
                if (!string.IsNullOrEmpty(pListaItemsBusqueda))
                {
                    string[] items = pListaItemsBusqueda.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string item in items)
                    {
                        mCargadorResultadosModel.ListaItemsBusqueda.Add(item);
                    }
                }
                if (!string.IsNullOrEmpty(pResultadosEliminar))
                {
                    mCargadorResultadosModel.ResultadosEliminar = pResultadosEliminar;
                }


                Dictionary<string, string> diccionarioPestanyas = new Dictionary<string, string>();
                foreach (ProyectoPestanyaBusqueda filaPestaña in mCargadorResultadosModel.PestanyasProyecto.ListaProyectoPestanyaBusqueda)
                {
                    if (!string.IsNullOrEmpty(filaPestaña.ProyectoPestanyaMenu.Ruta))
                    {
                        diccionarioPestanyas.Add(filaPestaña.ProyectoPestanyaMenu.Ruta.ToLower(), filaPestaña.CampoFiltro);
                    }
                }
                if (!string.IsNullOrEmpty(pUrlPaginaBusqueda))
                {
                    if (diccionarioPestanyas.ContainsKey(pUrlPaginaBusqueda.ToLower().Substring(pUrlPaginaBusqueda.LastIndexOf("/") + 1)))
                    {
                        pParametrosAdicionales += diccionarioPestanyas[pUrlPaginaBusqueda.ToLower().Substring(pUrlPaginaBusqueda.LastIndexOf("/") + 1)];
                    }
                } 

                #endregion

                #region Buscamos resultados

                AsignarPropiedadesCargadorResultadosModel();

                bool esMovil = mControladorBase.RequestParams("esMovil") == "true";

                mUtilServicioResultados.CargarResultadosInt(proyectoID, identidadID, identidadID != UsuarioAD.Invitado, pEsUsuarioInvitado, (TipoBusqueda)pTipoBusqueda, pGrafo, pParametros, pParametrosAdicionales, pPrimeraCarga, pLanguageCode, -1, pNumRecursosPagina, tipoFicha, pFiltroContexto, false, mCargadorResultadosModel, esMovil);

                #endregion

                #region Cargamos los resultados
                List<Guid> listaRecursos = new List<Guid>();
                foreach (string idResultado in mCargadorResultadosModel.ListaIdsResultado.Keys)
                {
                    switch (mCargadorResultadosModel.ListaIdsResultado[idResultado])
                    {
                        case TiposResultadosMetaBuscador.Documento:
                        case TiposResultadosMetaBuscador.DocumentoBRPrivada:
                        case TiposResultadosMetaBuscador.DocumentoBRPersonal:
                        case TiposResultadosMetaBuscador.Pregunta:
                        case TiposResultadosMetaBuscador.Debate:
                        case TiposResultadosMetaBuscador.Encuesta:
                            listaRecursos.Add(new Guid(idResultado));
                            break;
                    }
                }

                string baseUrlBusqueda = "";
                if (!string.IsNullOrEmpty(pUrlPaginaBusqueda))
                {
                    baseUrlBusqueda = pUrlPaginaBusqueda;
                }
                else
                {
                    baseUrlBusqueda = UrlPaginaActual;
                }
                if (baseUrlBusqueda.Contains("?"))
                {
                    baseUrlBusqueda = baseUrlBusqueda.Substring(0, baseUrlBusqueda.IndexOf("?"));
                }
                ControladorProyectoMVC controladorMVC = new ControladorProyectoMVC(UtilIdiomas, BaseURL, BaseURLsContent, BaseURLStatic, mCargadorResultadosModel.Proyecto, mCargadorResultadosModel.ProyectoOrigenID, mCargadorResultadosModel.FilaParametroGeneral, mCargadorResultadosModel.IdentidadActual, mCargadorResultadosModel.EsBot, mLoggingService, mEntityContext, mConfigService, mHttpContextAccessor, mRedisCacheWrapper, mVirtuosoAD, mGnossCache, mEntityContextBASE, mServicesUtilVirtuosoAndReplication);

                #region Recursos
                Dictionary<Guid, ResourceModel> listaResultados = controladorMVC.ObtenerRecursosPorID(listaRecursos, baseUrlBusqueda, null, true);
                UtilServicioResultados.ProcesarFichasRecursoParaPresentacion(listaResultados);
                #endregion

                #endregion

                List<ResourceModel> listaRecursosDevolver = new List<ResourceModel>();
                foreach (Guid id in listaResultados.Keys)
                {
                    listaRecursosDevolver.Add(listaResultados[id]);
                }
                mLoggingService.AgregarEntrada("Carga de contexto finalizada");

                JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    TypeNameHandling = TypeNameHandling.All
                };
                string respuesta = JsonConvert.SerializeObject(listaRecursosDevolver, jsonSerializerSettings);
                return Content(respuesta);
                //return Json(listaRecursosDevolver, new JsonSerializerSettings());
            }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                string url = HttpContext.Request.Path;
                if (!url.Contains("?"))
                {
                    url += "?" + HttpContext.Request.Query.ToString();
                }

                mUtilServicios.EnviarErrorYGuardarLog("Error: " + ex.Message + "\r\nPila: " + ex.StackTrace + "\r\nLlamada: " + url, "errorBots", mCargadorResultadosModel.EsBot);
            }

            return null;
        }
        [HttpGet, HttpPost]
        [Route("CargarResultadosGadget")]
        public ActionResult CargarResultadosGadget([FromForm] string pProyectoID, [FromForm] bool pEstaEnProyecto, [FromForm] bool pEsUsuarioInvitado, [FromForm] string pIdentidadID, [FromForm] string pParametros, [FromForm] bool pPrimeraCarga, [FromForm] string pLanguageCode, [FromForm] short pTipoBusqueda, [FromForm] int pNumResultados, [FromForm] string pUrlPaginaBusqueda, [FromForm] short pTipoFichaResultados, [FromForm] bool pJson, [FromForm] bool pUsarMasterParaLectura, [FromForm] bool pObtenerDatosExtraRecursos = true, [FromForm] bool pObtenerIdentidades = true, [FromForm] bool pObtenerDatosExtraIdentidades = false)
        {
            try
            {
                #region Obtenemos parametros
                mCargadorResultadosModel.EsPeticionGadget = true;
                mCargadorResultadosModel.SinCache = true;

                pProyectoID = pProyectoID.Replace("\"", "");
                pIdentidadID = pIdentidadID.Replace("\"", "");
                string grafo = pProyectoID;
                pParametros = pParametros.Replace("\"", "");
                pLanguageCode = pLanguageCode.Replace("\"", "");

                if (pUrlPaginaBusqueda == null)
                {
                    pUrlPaginaBusqueda = "";
                }
                pUrlPaginaBusqueda = pUrlPaginaBusqueda.Replace("\"", "");

                Guid proyectoID = new Guid(pProyectoID);
                Guid identidadID = new Guid(pIdentidadID);
                mCargadorResultadosModel.LanguageCode = pLanguageCode;
                mCargadorResultadosModel.ProyectoSeleccionado = proyectoID;

                string[] parametroslista = pParametros.Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                string parametrosAdicionales = "";

                foreach (string parametro in parametroslista)
                {
                    if (parametro.StartsWith("parametrosAdicionales:"))
                    {
                        parametrosAdicionales = parametro.Substring(parametro.IndexOf(":") + 1);
                    }
                    else
                    {
                        pParametros = parametro;
                    }
                }

                foreach (string parametro in parametroslista)
                {
                    if (parametro.StartsWith("listadoRecursosEstatico:"))
                    {
                        string[] idRecursos = parametro.Substring(pParametros.IndexOf(":") + 1).Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                        mCargadorResultadosModel.ListaIdsResultado = new Dictionary<string, TiposResultadosMetaBuscador>();
                        foreach (string idRecurso in idRecursos)
                        {
                            mCargadorResultadosModel.ListaIdsResultado.Add(idRecurso, TiposResultadosMetaBuscador.Documento);
                        }
                        pParametros = "";
                    }
                    else if (parametro.StartsWith("parametrosAdicionales:"))
                    {
                        parametrosAdicionales = parametro.Substring(parametro.IndexOf(":") + 1);
                    }
                    else
                    {
                        pParametros = parametro;
                    }
                }

                #region ProyectoVirtualID

                Guid proyectoVirtualID = proyectoID;

                if (parametrosAdicionales.Contains("proyectoVirtualID="))
                {
                    string trozo1 = parametrosAdicionales.Substring(0, parametrosAdicionales.IndexOf("proyectoVirtualID="));
                    string trozoProyOrgien = parametrosAdicionales.Substring(parametrosAdicionales.IndexOf("proyectoVirtualID="));
                    string trozo2 = trozoProyOrgien.Substring(trozoProyOrgien.IndexOf("|") + 1);
                    trozoProyOrgien = trozoProyOrgien.Substring(0, trozoProyOrgien.IndexOf("|"));

                    proyectoVirtualID = new Guid(trozoProyOrgien.Substring(trozoProyOrgien.IndexOf("=") + 1));
                    parametrosAdicionales = trozo1 + trozo2;
                }
                #endregion

                #endregion

                #region Buscamos resultados

                AsignarPropiedadesCargadorResultadosModel();

                bool esMovil = mControladorBase.RequestParams("esMovil") == "true";

                mUtilServicioResultados.CargarResultadosInt(proyectoID, identidadID, pEstaEnProyecto, pEsUsuarioInvitado, (TipoBusqueda)pTipoBusqueda, grafo, pParametros, parametrosAdicionales, pPrimeraCarga, pLanguageCode, -1, pNumResultados, (TipoFichaResultados)pTipoFichaResultados, "", false, mCargadorResultadosModel, esMovil);
                #endregion

                #region Cargamos los resultados
                List<Guid> listaRecursosID = new List<Guid>();
                foreach (string idResultado in mCargadorResultadosModel.ListaIdsResultado.Keys)
                {
                    listaRecursosID.Add(new Guid(idResultado));
                }

                string baseUrlBusqueda = "";
                if (!string.IsNullOrEmpty(pUrlPaginaBusqueda))
                {
                    baseUrlBusqueda = pUrlPaginaBusqueda;
                }
                else
                {
                    baseUrlBusqueda = UrlPaginaActual;
                }
                if (baseUrlBusqueda.Contains("?"))
                {
                    baseUrlBusqueda = baseUrlBusqueda.Substring(0, baseUrlBusqueda.IndexOf("?"));
                }
                ControladorProyectoMVC controladorMVC = new ControladorProyectoMVC(UtilIdiomas, BaseURL, BaseURLsContent, BaseURLStatic, mCargadorResultadosModel.Proyecto, mCargadorResultadosModel.ProyectoOrigenID, mCargadorResultadosModel.FilaParametroGeneral, mCargadorResultadosModel.IdentidadActual, mCargadorResultadosModel.EsBot, mLoggingService, mEntityContext, mConfigService, mHttpContextAccessor, mRedisCacheWrapper, mVirtuosoAD, mGnossCache, mEntityContextBASE, mServicesUtilVirtuosoAndReplication);

                Dictionary<Guid, ResourceModel> listaRecursosModel = controladorMVC.ObtenerRecursosPorID(listaRecursosID, baseUrlBusqueda, null, true, pObtenerIdentidades, pObtenerDatosExtraIdentidades);
                UtilServicioResultados.ProcesarFichasRecursoParaPresentacion(listaRecursosModel);

                #endregion

                ViewBag.UtilIdiomas = UtilIdiomas;
                ViewBag.EsCatalogo = mCargadorResultadosModel.Proyecto.EsCatalogo;
                ViewBag.MostrarPersonasEnCatalogo = mCargadorResultadosModel.FilaParametroGeneral.MostrarPersonasEnCatalogo;
                ViewBag.ProyectoID = proyectoID;

                List<ObjetoBuscadorModel> ListaResultados = new List<ObjetoBuscadorModel>();
                foreach (ResourceModel fichaRecurso in listaRecursosModel.Values)
                {
                    ListaResultados.Add(fichaRecurso);
                }

                ResultadoModel resultadoModel = new ResultadoModel();
                resultadoModel.ListaResultados = ListaResultados;
                resultadoModel.NumeroResultadosTotal = mCargadorResultadosModel.NumeroResultados;
                resultadoModel.NumeroResultadosPagina = mCargadorResultadosModel.FilasPorPagina;
                resultadoModel.NumeroPaginaActual = mCargadorResultadosModel.PaginaActual;
                resultadoModel.UrlBusqueda = mCargadorResultadosModel.UrlNavegador;
                ViewData.Model = resultadoModel;

                CargarPersonalizacion(proyectoVirtualID, controladorMVC);

                string funcionCallBack = HttpContext.Request.Query["callback"];

                if (string.IsNullOrEmpty(funcionCallBack))
                {
                    if (pJson)
                    {
                        // string respuesta = System.Text.Json.JsonSerializer.Serialize(resultadoModel);

                        var settings = new Newtonsoft.Json.JsonSerializerSettings
                        {
                            TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects,
                            Formatting = Newtonsoft.Json.Formatting.Indented
                        };
                        string respuesta = JsonConvert.SerializeObject(resultadoModel, settings);
                        return Content(respuesta);
                    }
                    else
                    {
                        //return View("CargarResultados");

                        string resultado = "";
                        using (StringWriter sw = new StringWriter())
                        {
                            ViewEngineResult viewResult = mViewEngine.FindView(ControllerContext, ObtenerNombreVista("CargarResultadosGadget"), false);

                            if (viewResult.View == null) throw new Exception("View not found: CargarResultadosGadget");
                            ViewContext viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw, new HtmlHelperOptions());
                            viewResult.View.RenderAsync(viewContext);

                            resultado = sw.GetStringBuilder().ToString();
                        }

                        KeyValuePair<int, string> resultadoPeticion = new KeyValuePair<int, string>(resultadoModel.NumeroResultadosTotal, resultado);

                        //Devuelvo la respuesta en el response de la petición
                        return Content(System.Text.Json.JsonSerializer.Serialize(resultadoPeticion));
                    }
                }
                else
                {
                    string resultado = "";
                    using (StringWriter sw = new StringWriter())
                    {
                        ViewEngineResult viewResult = mViewEngine.FindView(ControllerContext, ObtenerNombreVista("CargarResultadosGadget"), false);

                        if (viewResult.View == null) throw new Exception("View not found: CargarResultadosGadget");
                        ViewContext viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw, new HtmlHelperOptions());
                        viewResult.View.RenderAsync(viewContext);

                        resultado = sw.GetStringBuilder().ToString();
                    }

                    //Devuelvo la respuesta en el response de la petición

                    HttpContext.Response.ContentType = "text/plain";
                    HttpContext.Response.WriteAsync(funcionCallBack + "({\"d\":" + System.Text.Json.JsonSerializer.Serialize(resultado) + "});");
                }

            }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                string url = HttpContext.Request.Path;
                if (!url.Contains("?"))
                {
                    url += "?" + HttpContext.Request.Query.ToString();
                }

                mUtilServicios.EnviarErrorYGuardarLog("Error: " + ex.Message + "\r\nPila: " + ex.StackTrace + "\r\nLlamada: " + url, "error", mCargadorResultadosModel.EsBot);
            }

            return new EmptyResult();
        }
        [HttpGet, HttpPost]
        [Route("CargarResultadosGadgetSPARQL")]
        public ActionResult CargarResultadosGadgetSPARQL([FromForm] string pSPARQL, [FromForm] Guid pProyectoID, [FromForm] int pNumItemsPag, [FromForm] int pNumPag, [FromForm] string pLanguageCode, [FromForm] bool pObtenerDatosExtraRecursos = true, [FromForm] bool pObtenerIdentidades = true, [FromForm] bool pObtenerDatosExtraIdentidades = false)
        {
            try
            {
                mCargadorResultadosModel.LanguageCode = pLanguageCode;
                mCargadorResultadosModel.ProyectoSeleccionado = pProyectoID;
                mCargadorResultadosModel.FilasPorPagina = pNumItemsPag;
                mCargadorResultadosModel.PaginaActual = pNumPag;
                mCargadorResultadosModel.IdentidadID = UsuarioAD.Invitado;
                mCargadorResultadosModel.TipoProyecto = mCargadorResultadosModel.Proyecto.TipoProyecto;
                mCargadorResultadosModel.IdentidadActual = mControladorBase.ObtenerIdentidadUsuarioInvitado(UtilIdiomas).ListaIdentidades[UsuarioAD.Invitado];
                mCargadorResultadosModel.PerfilIdentidadID = UsuarioAD.Invitado;
                mCargadorResultadosModel.UrlPerfil = "/";
                mCargadorResultadosModel.SinCache = true;


                DataWrapperFacetas tConfiguracionOntologia = new DataWrapperFacetas();

                mCargadorResultadosModel.InformacionOntologias = mUtilServiciosFacetas.ObtenerInformacionOntologias(mCargadorResultadosModel.Proyecto.FilaProyecto.OrganizacionID, mCargadorResultadosModel.ProyectoSeleccionado, tConfiguracionOntologia);

                mCargadorResultadosModel.TConfiguracionOntologia = tConfiguracionOntologia;

                mCargadorResultadosModel.FacetadoCL = new FacetadoCL(mUtilServicios.UrlIntragnoss, mCargadorResultadosModel.AdministradorQuiereVerTodasLasPersonas, mCargadorResultadosModel.ProyectoSeleccionado.ToString().ToLower(), true, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
                mCargadorResultadosModel.FacetadoCL.InformacionOntologias = mCargadorResultadosModel.InformacionOntologias;
                mCargadorResultadosModel.FacetadoDS = new FacetadoDS();

                #region Buscamos resultados

                #region Resultados
                string sparqlBusqueda = pSPARQL;
                sparqlBusqueda += " LIMIT " + pNumItemsPag;
                if (pNumPag > 0)
                {
                    sparqlBusqueda += " OFFSET " + pNumItemsPag * (pNumPag - 1);
                }
                mCargadorResultadosModel.FacetadoCL.FacetadoCN.LeerDeVirtuoso("SPARQL " + sparqlBusqueda, "RecursosBusqueda", mCargadorResultadosModel.FacetadoDS, mCargadorResultadosModel.ProyectoSeleccionado.ToString().ToLower());
                #endregion

                #region Num resultados
                string sparqlNumero = pSPARQL;
                int indexOrderBy = sparqlNumero.ToLower().IndexOf("order by ");
                int indexSelect = sparqlNumero.ToLower().IndexOf("select ");
                int indexFrom = sparqlNumero.ToLower().IndexOf("from ");
                if (indexOrderBy > -1)
                {
                    sparqlNumero = sparqlNumero.Substring(0, indexOrderBy);
                }
                if (indexSelect > -1 && indexFrom > -1)
                {
                    sparqlNumero = sparqlNumero.Substring(0, indexSelect) + " select (count(distinct ?s)) " + sparqlNumero.Substring(indexFrom);
                }
                mCargadorResultadosModel.FacetadoCL.FacetadoCN.LeerDeVirtuoso("SPARQL " + sparqlNumero, "NResultadosBusqueda", mCargadorResultadosModel.FacetadoDS, mCargadorResultadosModel.ProyectoSeleccionado.ToString().ToLower());
                #endregion

                if (mCargadorResultadosModel.FacetadoDS.Tables.Contains("NResultadosBusqueda"))
                {
                    //Número total de resultados de la búsqueda, sólo se obtienen en la primera petición
                    object numeroResultados = mCargadorResultadosModel.FacetadoDS.Tables["NResultadosBusqueda"].Rows[0][0];
                    if (numeroResultados is long)
                    {
                        mCargadorResultadosModel.NumeroResultados = (int)(long)numeroResultados;
                    }
                    else if (numeroResultados is int)
                    {
                        mCargadorResultadosModel.NumeroResultados = (int)numeroResultados;
                    }
                    else if (numeroResultados is string)
                    {
                        int numeroResultadosInt;
                        int.TryParse((string)numeroResultados, out numeroResultadosInt);
                        mCargadorResultadosModel.NumeroResultados = numeroResultadosInt;
                    }
                }

                //carga una lista con cada ID y el tipo del elemento
                if (mCargadorResultadosModel.ListaIdsResultado == null)
                {
                    mCargadorResultadosModel.ObtenerListaID();
                }
                #endregion

                #region Cargamos los resultados
                List<Guid> listaRecursosID = new List<Guid>();
                foreach (string idResultado in mCargadorResultadosModel.ListaIdsResultado.Keys)
                {
                    listaRecursosID.Add(new Guid(idResultado));
                }

                ControladorProyectoMVC controladorMVC = new ControladorProyectoMVC(UtilIdiomas, BaseURL, BaseURLsContent, BaseURLStatic, mCargadorResultadosModel.Proyecto, mCargadorResultadosModel.ProyectoOrigenID, mCargadorResultadosModel.FilaParametroGeneral, mCargadorResultadosModel.IdentidadActual, mCargadorResultadosModel.EsBot, mLoggingService, mEntityContext, mConfigService, mHttpContextAccessor, mRedisCacheWrapper, mVirtuosoAD, mGnossCache, mEntityContextBASE, mServicesUtilVirtuosoAndReplication);

                Dictionary<Guid, ResourceModel> listaRecursosModel = controladorMVC.ObtenerRecursosPorID(listaRecursosID, "", null, true, pObtenerIdentidades, pObtenerDatosExtraIdentidades);
                UtilServicioResultados.ProcesarFichasRecursoParaPresentacion(listaRecursosModel);

                #endregion

                ViewBag.UtilIdiomas = UtilIdiomas;
                ViewBag.EsCatalogo = mCargadorResultadosModel.Proyecto.EsCatalogo;
                ViewBag.MostrarPersonasEnCatalogo = mCargadorResultadosModel.FilaParametroGeneral.MostrarPersonasEnCatalogo;
                ViewBag.ProyectoID = mCargadorResultadosModel.Proyecto.Clave;

                List<ObjetoBuscadorModel> ListaResultados = new List<ObjetoBuscadorModel>();
                foreach (ResourceModel fichaRecurso in listaRecursosModel.Values)
                {
                    ListaResultados.Add(fichaRecurso);
                }

                ResultadoModel resultadoModel = new ResultadoModel();
                resultadoModel.ListaResultados = ListaResultados;
                resultadoModel.NumeroResultadosTotal = mCargadorResultadosModel.NumeroResultados;
                resultadoModel.NumeroResultadosPagina = mCargadorResultadosModel.FilasPorPagina;
                resultadoModel.NumeroPaginaActual = mCargadorResultadosModel.PaginaActual;
                resultadoModel.UrlBusqueda = mCargadorResultadosModel.UrlNavegador;
                ViewData.Model = resultadoModel;

                CargarPersonalizacion(mCargadorResultadosModel.Proyecto.Clave, controladorMVC);

                JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                };
                string respuesta = JsonConvert.SerializeObject(resultadoModel, jsonSerializerSettings);
                return Content(respuesta);

            }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                string url = HttpContext.Request.Path;
                if (!url.Contains("?"))
                {
                    url += "?" + HttpContext.Request.Query.ToString();
                }

                mUtilServicios.EnviarErrorYGuardarLog("Error: " + ex.Message + "\r\nPila: " + ex.StackTrace + "\r\nLlamada: " + url, "error", mCargadorResultadosModel.EsBot);
            }

            return new EmptyResult();
        }
        [HttpGet, HttpPost]
        [Route("RefrescarResultados")]
        public void RefrescarResultados(Guid pProyectoID, bool pEsMyGnoss, bool pEstaEnProyecto, bool pEsUsuarioInvitado, bool pPrimeraCarga, string pLanguageCode, short pTipoBusqueda, int pNumeroResultados, bool pEsBot, string pParametros_adiccionales)
        {
            try
            {
                if (Guid.Empty.Equals(pProyectoID))
                {
                    pProyectoID = Guid.Parse(Request.Form["pProyectoID"]);
                    pEsMyGnoss = Request.Form["pEsMyGnoss"].ToString().ToLower() == "true" ? true : false;
                    pEstaEnProyecto = Request.Form["pEstaEnProyecto"].ToString().ToLower() == "true" ? true : false;
                    pEsUsuarioInvitado = Request.Form["pEsUsuarioInvitado"].ToString().ToLower() == "true" ? true : false;
                    pPrimeraCarga = Request.Form["pPrimeraCarga"].ToString().ToLower() == "true" ? true : false;
                    pLanguageCode = Request.Form["pLanguageCode"];
                    pTipoBusqueda = short.Parse(Request.Form["pTipoBusqueda"]);
                    pNumeroResultados = int.Parse(Request.Form["pNumeroResultados"]);
                    pEsBot = Request.Form["pEsBot"].ToString().ToLower() == "true" ? true : false;
                    pParametros_adiccionales = Request.Form["pParametros_adiccionales"];
                }


                mCargadorResultadosModel.EsRefrescoCache = true;

                string parametros = "";
                string identidadID = UsuarioAD.Invitado.ToString();
                string grafo = "";
                if (pTipoBusqueda.Equals((short)TipoBusqueda.Mensajes) && !string.IsNullOrEmpty(pParametros_adiccionales))
                {
                    parametros = pParametros_adiccionales.Split('|')[0];
                    identidadID = pParametros_adiccionales.Split('|')[1];
                    grafo = pParametros_adiccionales.Split('|')[2];

                    pParametros_adiccionales = "";
                }

                mCargadorResultadosModel.ProyectoSeleccionado = pProyectoID;

                AsignarPropiedadesCargadorResultadosModel();

                bool esMovil = mControladorBase.RequestParams("esMovil") == "true";

                mUtilServicioResultados.CargarResultadosInt(pProyectoID, new Guid(identidadID), pEstaEnProyecto, pEsUsuarioInvitado, (TipoBusqueda)pTipoBusqueda, grafo, parametros, pParametros_adiccionales, pPrimeraCarga, pLanguageCode, pNumeroResultados, mCargadorResultadosModel.FilasPorPagina, TipoFichaResultados.Completa, "", false, mCargadorResultadosModel, esMovil);
            }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                string url = HttpContext.Request.Path;
                if (!url.Contains("?"))
                {
                    url += "?" + HttpContext.Request.Query.ToString();
                }

                mUtilServicios.EnviarErrorYGuardarLog("Error: " + ex.Message + "\r\nPila: " + ex.StackTrace + "\r\nLlamada: " + url, "error", mCargadorResultadosModel.EsBot);
            }
        }

        [HttpGet, HttpPost]
        [Route("CargarResultadosObjetoGnoss")]
        public ActionResult CargarResultadosObjetoGnoss(Guid pProyectoID, bool pEstaEnProyecto, bool pEsUsuarioInvitado, Guid pIdentidadID, string pParametros, bool pPrimeraCarga, string pLanguageCode, short pTipoBusqueda, int pNumResultados, string pGrafo, short pTipoFichaResultados)
        {
            try
            {
                //mCargadorResultadosModel.EsPeticionGadget = true;
                //mDiseñoRecursos = "";
                //mCargadorResultadosModel.SinCache = true;

                //string[] parametroslista = pParametros.Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                //string parametrosAdicionales = "";

                //foreach (string parametro in parametroslista)
                //{
                //    if (parametro.StartsWith("listadoRecursosEstatico:"))
                //    {
                //        string[] idRecursos = parametro.Substring(pParametros.IndexOf(":") + 1).Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                //        mCargadorResultadosModel.ListaIdsResultado = new Dictionary<string, TiposResultadosMetaBuscador>();
                //        foreach (string idRecurso in idRecursos)
                //        {
                //            mCargadorResultadosModel.ListaIdsResultado.Add(idRecurso, TiposResultadosMetaBuscador.Documento);
                //        }
                //        pParametros = "";
                //    }
                //    else if (parametro.StartsWith("parametrosAdicionales:"))
                //    {
                //        parametrosAdicionales = parametro.Substring(parametro.IndexOf(":") + 1);
                //    }
                //    else
                //    {
                //        pParametros = parametro;
                //    }
                //}
                //CargarResultadosInt(pProyectoID,pIdentidadID,pEstaEnProyecto,pEsUsuarioInvitado,(TipoBusqueda)pTipoBusqueda,pGrafo, pParametros, parametrosAdicionales, pPrimeraCarga, pLanguageCode,-1,pNumResultados,(TipoFichaResultados)pTipoFichaResultados,"",false);

            }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                string url = HttpContext.Request.Path;
                if (!url.Contains("?"))
                {
                    url += "?" + HttpContext.Request.Query.ToString();
                }

                mUtilServicios.EnviarErrorYGuardarLog("Error: " + ex.Message + "\r\nPila: " + ex.StackTrace + "\r\nLlamada: " + url, "error", mCargadorResultadosModel.EsBot);
            }

            return new EmptyResult();
        }

        /// <summary>
        /// Obtiene la ficha de un recurso.
        /// </summary>
        /// <param name="identidad">Identidad con la que está conectada el usuario</param>
        /// <param name="languageCode">Language code del idioma con el que esta conectado el usuario (es, en...)</param>
        /// <param name="proyecto">Proyecto en el que se está realizando la búsqueda</param>
        /// <param name="documentoID">ID o IDs separdos por comas de los documentos a buscar</param>
        /// <param name="urlBusqueda">Url modelo para búsquedas configuradas dentro de la búsqueda</param>
        /// <returns>HTML con los resultados</returns>
        [HttpGet, HttpPost]
        [Route("ObtenerFichaRecurso")]
        public ActionResult ObtenerFichaRecurso([FromForm] string proyecto, [FromForm] string identidad, [FromForm] string languageCode, [FromForm] string documentoID, [FromForm] string urlBusqueda, [FromForm] Guid pPersonaID)
        {
            try
            {
                mLoggingService.GuardarLogError($"Entra a ObtenerFichaRecurso: \n proyecto -> {proyecto}, \n identidad -> {identidad}, \n languageCode -> {languageCode}, \n documentoID -> {documentoID}, \n urlBusqueda -> {urlBusqueda}, \n pPersonaID -> {pPersonaID.ToString()}");
                proyecto = proyecto.Replace("\"", "");
                mCargadorResultadosModel.ProyectoSeleccionado = new Guid(proyecto);
                mCargadorResultadosModel.LanguageCode = languageCode.Replace("\"", "");
                identidad = identidad.Replace("\"", "");
                documentoID = documentoID.Replace("\"", "");
                if (!string.IsNullOrEmpty(urlBusqueda))
                {
                    urlBusqueda = urlBusqueda.Replace("\"", "");
                }

                if (mCargadorResultadosModel.ProyectoSeleccionado != ProyectoAD.MetaProyecto || mCargadorResultadosModel.ProyectoMyGnoss == null)
                {
                    mCargadorResultadosModel.TipoProyecto = mCargadorResultadosModel.Proyecto.TipoProyecto;

                    if (mCargadorResultadosModel.ProyectoSeleccionado == ProyectoAD.MetaProyecto)
                    {
                        mCargadorResultadosModel.ProyectoMyGnoss = mCargadorResultadosModel.Proyecto;
                    }
                }

                Guid identidadID = new Guid(identidad);
                if (identidadID.Equals(UsuarioAD.Invitado))
                {
                    GestionIdentidades gestorIdentidades = new GestionIdentidades(new DataWrapperIdentidad(), mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication);
                    mControladorBase.CrearIdentidadUsuarioInvitadoParaPerfil(gestorIdentidades, UsuarioAD.Invitado, ProyectoAD.MetaOrganizacion, mCargadorResultadosModel.ProyectoSeleccionado, pPersonaID);
                    mCargadorResultadosModel.IdentidadActual = gestorIdentidades.ListaIdentidades[identidadID];
                }
                else
                {
                    CargarIdentidad(new Guid(identidad));
                }

                ControladorProyectoMVC controladorMVC = new ControladorProyectoMVC(UtilIdiomas, BaseURL, BaseURLsContent, BaseURLStatic, mCargadorResultadosModel.Proyecto, mCargadorResultadosModel.ProyectoOrigenID, mCargadorResultadosModel.FilaParametroGeneral, mCargadorResultadosModel.IdentidadActual, mCargadorResultadosModel.EsBot, mLoggingService, mEntityContext, mConfigService, mHttpContextAccessor, mRedisCacheWrapper, mVirtuosoAD, mGnossCache, mEntityContextBASE, mServicesUtilVirtuosoAndReplication);
                Dictionary<Guid, ResourceModel> listaRecursosModel = new Dictionary<Guid, ResourceModel>();
                List<Guid> listaRecursosID = new List<Guid>();

                foreach (string docID in documentoID.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    listaRecursosID.Add(new Guid(docID));
                }

                if (listaRecursosID.Count == 0)
                {
                    return new EmptyResult();
                }

                mCargadorResultadosModel.FacetadoCL = new FacetadoCL(mUtilServicios.UrlIntragnoss, mCargadorResultadosModel.AdministradorQuiereVerTodasLasPersonas, mCargadorResultadosModel.GrafoID, true, mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);

                if (mCargadorResultadosModel.ProyectoSeleccionado != ProyectoAD.MetaProyecto)
                {
                    //PRIVACIDAD: Descomentar este código cuando se quieran volver a mostrar recursos privados y públicos de comunidades privadas
                    //mCargadorResultadosModel.FacetadoCL.ListaComunidadesPrivadasUsuario = UtilServiciosFacetas.ObtenerListaComunidadesPrivadasUsuario(mCargadorResultadosModel.IdentidadID, mCargadorResultadosModel.EsUsuarioInvitado);
                    mCargadorResultadosModel.FacetadoCL.ListaItemsBusquedaExtra = mCargadorResultadosModel.ListaItemsBusquedaExtra;

                    DataWrapperFacetas tConfiguracionOntologia = new DataWrapperFacetas();

                    mCargadorResultadosModel.FacetadoCL.InformacionOntologias = mUtilServiciosFacetas.ObtenerInformacionOntologias(mCargadorResultadosModel.Proyecto.FilaProyecto.OrganizacionID, mCargadorResultadosModel.ProyectoSeleccionado, tConfiguracionOntologia);

                    mCargadorResultadosModel.TConfiguracionOntologia = tConfiguracionOntologia;

                    mCargadorResultadosModel.FacetadoCL.PropiedadesRango = mUtilServiciosFacetas.ObtenerPropiedadesRango(mCargadorResultadosModel.GestorFacetas);
                    mCargadorResultadosModel.FacetadoCL.PropiedadesFecha = mUtilServiciosFacetas.ObtenerPropiedadesFecha(mCargadorResultadosModel.GestorFacetas);
                }

                mCargadorResultadosModel.InformacionOntologias = mCargadorResultadosModel.FacetadoCL.InformacionOntologias;

                listaRecursosModel = controladorMVC.ObtenerRecursosPorID(listaRecursosID, urlBusqueda, null, !mCargadorResultadosModel.SinDatosExtra);
                UtilServicioResultados.ProcesarFichasRecursoParaPresentacion(listaRecursosModel);

                ViewBag.UtilIdiomas = UtilIdiomas;
                ViewBag.EsCatalogo = mCargadorResultadosModel.Proyecto.EsCatalogo;
                ViewBag.MostrarPersonasEnCatalogo = mCargadorResultadosModel.FilaParametroGeneral.MostrarPersonasEnCatalogo;
                ViewBag.ProyectoID = mCargadorResultadosModel.Proyecto.Clave;
                ViewBag.BaseUrlContent = BaseURLsContent[0];
                ViewBag.GeneradorURLs = mControladorBase.UrlsSemanticas;
                List<ObjetoBuscadorModel> ListaResultados = new List<ObjetoBuscadorModel>();

                foreach (ResourceModel resource in listaRecursosModel.Values)
                {
                    resource.MapView = true;
                    ListaResultados.Add(resource);
                }

                ResultadoModel resultadoModel = new ResultadoModel();
                resultadoModel.NumeroResultadosTotal = -1;
                resultadoModel.NumeroResultadosPagina = mCargadorResultadosModel.FilasPorPagina;
                resultadoModel.NumeroPaginaActual = mCargadorResultadosModel.PaginaActual;
                resultadoModel.UrlBusqueda = mCargadorResultadosModel.UrlNavegador;
                resultadoModel.ListaResultados = ListaResultados;
                resultadoModel.MapView = true;
                ViewData.Model = resultadoModel;

                CargarPersonalizacion(mCargadorResultadosModel.ProyectoSeleccionado, controladorMVC);

                string funcionCallBack = HttpContext.Request.Query["callback"];
                string resultado = "";
                using (StringWriter sw = new StringWriter())
                {
                    ViewEngineResult viewResult = mViewEngine.FindView(ControllerContext, ObtenerNombreVista("CargarResultados"), false);

                    if (viewResult.View == null) throw new Exception("View not found: _ResultadoRecurso");
                    ViewContext viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw, new HtmlHelperOptions());
                    viewResult.View.RenderAsync(viewContext);

                    resultado = sw.GetStringBuilder().ToString();
                }

                //Devuelvo la respuesta en el response de la petición

                HttpContext.Response.ContentType = "text/plain";
                HttpContext.Response.WriteAsync(funcionCallBack + "({\"d\":" + System.Text.Json.JsonSerializer.Serialize(resultado) + "});");
            }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                string url = HttpContext.Request.Path;
                if (!url.Contains("?"))
                {
                    url += "?" + HttpContext.Request.Query.ToString();
                }

                mUtilServicios.EnviarErrorYGuardarLog("Error: " + ex.Message + "\r\nPila: " + ex.StackTrace + "\r\nLlamada: " + url, "error", mCargadorResultadosModel.EsBot);
            }
            return new EmptyResult();
        }


        #endregion

        #region Metodos generales

        #region Métodos de control

        /// <summary>
        /// Carga una identidad cuya clave se pasa por parametro
        /// </summary>
        /// <param name="pIdentidad">Identificador de la identidad que se desea cargar</param>
        /// <returns>Devuelve la identidad cuya clave se pasa por parametro</returns>
        private void CargarIdentidad(Guid pIdentidad)
        {
            mCargadorResultadosModel.IdentidadActual = mUtilServicioResultados.CargarIdentidad(pIdentidad);
        }

        /// <summary>
        /// Obtiene el contenido de la etiqueta pasada como parámetro
        /// </summary>
        /// <param name="pPage">Etiqueta de la página en el fichero de idioma </param>
        /// <param name="pText">Etiqueta del texto en el fichero de idioma</param>
        /// <returns>Cadena de texto con el contenido de la etiqueta solicitada</returns>
        private string GetText(string pPage, string pText)
        {
            return UtilIdiomas.GetText(pPage, pText);
        }

        /// <summary>
        /// Devuelve el valor mapeandolo según sea un rdf:type o gnoss:type.
        /// </summary>
        /// <param name="pValor">Valor</param>
        /// <returns>Valor mapeandolo según sea un rdf:type o gnoss:type</returns>
        private string ObtenerValorMapeandoTypeSubType(string pValor)
        {
            foreach (OntologiaProyecto filaOnto in mCargadorResultadosModel.GestorFacetas.FacetasDW.ListaOntologiaProyecto)
            {
                if (filaOnto.OntologiaProyecto1.ToLower() == pValor.ToLower())
                {
                    return UtilCadenas.ObtenerTextoDeIdioma(filaOnto.NombreOnt, mCargadorResultadosModel.LanguageCode, null);
                }

                if (!filaOnto.SubTipos.Equals(null) && !string.IsNullOrEmpty(filaOnto.SubTipos))
                {
                    string valorNamespaceado = FacetaAD.ObtenerValorAplicandoNamespaces(pValor, filaOnto, false);

                    if (filaOnto.SubTipos.Contains(valorNamespaceado))
                    {
                        return FacetaAD.ObtenerTextoSubTipoDeIdioma(valorNamespaceado, mCargadorResultadosModel.GestorFacetas.FacetasDW.ListaOntologiaProyecto, mCargadorResultadosModel.LanguageCode);
                    }
                }
            }

            return pValor;
        }

        #endregion

        #region Métodos auxiliares

        [NonAction]
        private void CargarPersonalizacion(Guid pProyectoID, ControladorProyectoMVC pControladorProyectoMVC)
        {
            CommunityModel comunidad = new CommunityModel();
            comunidad.ListaPersonalizaciones = new List<string>();
            comunidad.ListaPersonalizacionesDominio = new List<string>();
            comunidad.ListaPersonalizacionesEcosistema = new List<string>();

            if (pProyectoID != ProyectoAD.MetaProyecto)
            {
                comunidad.Url = new GnossUrlsSemanticas(mLoggingService, mEntityContext, mConfigService).ObtenerURLComunidad(UtilIdiomas, BaseURLIdioma, mCargadorResultadosModel.Proyecto.NombreCorto);
            }
            else
            {
                comunidad.Url = BaseURLIdioma;
            }

            if (mCargadorResultadosModel != null && mCargadorResultadosModel.Proyecto != null)
            {
                comunidad.ShortName = mCargadorResultadosModel.Proyecto.NombreCorto;
            }

            comunidad.Categories = CargarTesauroProyecto();
            comunidad.Key = pProyectoID;

            ViewBag.Comunidad = comunidad;

            string controllerName = this.ToString();
            controllerName = controllerName.Substring(controllerName.LastIndexOf('.') + 1);
            controllerName = controllerName.Substring(0, controllerName.IndexOf("Controller"));
            ViewBag.ControllerName = controllerName;

            UserIdentityModel identidad = new UserIdentityModel();
            identidad.IsGuestIdentity = mCargadorResultadosModel.IdentidadActual.Clave == UsuarioAD.Invitado;
            identidad.IsGuestUser = mCargadorResultadosModel.IdentidadActual.PersonaID == UsuarioAD.Invitado;
            identidad.KeyIdentity = mCargadorResultadosModel.IdentidadActual.Clave;

            if (mCargadorResultadosModel.IdentidadActual.PersonaID.HasValue)
            {
                identidad.KeyPerson = mCargadorResultadosModel.IdentidadActual.PersonaID.Value;
            }

            if (mCargadorResultadosModel.IdentidadActual.Persona != null)
            {
                identidad.KeyUser = mCargadorResultadosModel.IdentidadActual.Persona.UsuarioID;
                identidad.IsProyectAdmin = mCargadorResultadosModel.Proyecto.EsAdministradorUsuario(mCargadorResultadosModel.IdentidadActual.Persona.UsuarioID);
            }

            identidad.KeyProfile = mCargadorResultadosModel.IdentidadActual.PerfilID;
            identidad.IdentityGroups = CargarGruposIdentidadActual(pControladorProyectoMVC);

            ViewBag.IdentidadActual = identidad;

            ProyectoCN proyCN = new ProyectoCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            // las personalizaciones no se cargan en la página de administración de miembros
            if (!mCargadorResultadosModel.AdministradorQuiereVerTodasLasPersonas || !mCargadorResultadosModel.TipoBusqueda.Equals(TipoBusqueda.PersonasYOrganizaciones) || !proyCN.EsIdentidadAdministradorProyecto(mCargadorResultadosModel.IdentidadID, pProyectoID, TipoRolUsuario.Administrador))
            {
                VistaVirtualCL vistaVirtualCL = new VistaVirtualCL(mEntityContext, mLoggingService, mGnossCache, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
                DataWrapperVistaVirtual vistaVirtualDW = vistaVirtualCL.ObtenerVistasVirtualPorProyectoID(pProyectoID, PersonalizacionEcosistemaID, ComunidadExcluidaPersonalizacionEcosistema);

                Guid personalizacionProyecto = Guid.Empty;
                if (vistaVirtualDW.ListaVistaVirtualProyecto.Count > 0)
                {
                    personalizacionProyecto = ((VistaVirtualProyecto)vistaVirtualDW.ListaVistaVirtualProyecto.FirstOrDefault()).PersonalizacionID;
                }

                if (mUtilServicios.ComprobacionInvalidarVistasLocales(personalizacionProyecto, PersonalizacionEcosistemaID))
                {
                    vistaVirtualCL.InvalidarVistasVirtualesEnCacheLocal(pProyectoID);
                    vistaVirtualCL.InvalidarVistasVirtualesEcosistemaEnCacheLocal();
                    //Se ha invalidado la caché de vistas, cargo de nuevo el data set de vistas virtuales
                    vistaVirtualDW = vistaVirtualCL.ObtenerVistasVirtualPorProyectoID(pProyectoID, PersonalizacionEcosistemaID, ComunidadExcluidaPersonalizacionEcosistema);

                    mLoggingService.AgregarEntrada("Borramos las vistas del VirtualPathProvider");
                    BDVirtualPath.LimpiarListasRutasVirtuales();
                }

                if (mUtilServicios.ComprobacionInvalidarTraduccionesLocales(personalizacionProyecto, mControladorBase.PersonalizacionEcosistemaID))
                {
                    UtilIdiomas.CargarTextosPersonalizadosDominio("", mControladorBase.PersonalizacionEcosistemaID);
                }

                comunidad.PersonalizacionProyectoID = personalizacionProyecto;

                if (personalizacionProyecto != Guid.Empty)
                {
                    foreach (VistaVirtual filaVistaVirtual in vistaVirtualDW.ListaVistaVirtual.Where(item => item.PersonalizacionID.Equals(personalizacionProyecto)))
                    {
                        comunidad.ListaPersonalizaciones.Add(filaVistaVirtual.TipoPagina);
                    }

                    ViewBag.Personalizacion = "$$$" + personalizacionProyecto;
                }
                if (PersonalizacionEcosistemaID != Guid.Empty)
                {
                    foreach (VistaVirtual filaVistaVirtual in vistaVirtualDW.ListaVistaVirtual.Where(item => item.PersonalizacionID.Equals(PersonalizacionEcosistemaID)))
                    {
                        comunidad.ListaPersonalizacionesEcosistema.Add(filaVistaVirtual.TipoPagina);
                    }

                    ViewBag.PersonalizacionEcosistema = "$$$" + PersonalizacionEcosistemaID;
                }
            }
        }

        [NonAction]
        protected List<CategoryModel> CargarTesauroProyecto()
        {
            List<CategoryModel> listaCategoriasTesauro = new List<CategoryModel>();

            TesauroCL tesauroCL = new TesauroCL(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mServicesUtilVirtuosoAndReplication);
            DataWrapperTesauro tesauroDW = tesauroCL.ObtenerTesauroDeProyecto(mCargadorResultadosModel.ProyectoSeleccionado);

            GestionTesauro gestorTesauro = new GestionTesauro(tesauroDW, mLoggingService, mEntityContext);

            foreach (CategoriaTesauro catTes in gestorTesauro.ListaCategoriasTesauro.Values)
            {
                CategoryModel categoriaTesauro = CargarCategoria(catTes);
                listaCategoriasTesauro.Add(categoriaTesauro);
            }

            return listaCategoriasTesauro;
        }

        private List<GroupCardModel> CargarGruposIdentidadActual(ControladorProyectoMVC pControladorProyectoMVC)
        {
            List<GroupCardModel> listaGrupos = new List<GroupCardModel>();

            if (mCargadorResultadosModel.IdentidadActual.ListaGruposIdentidadParticipacion.Count > 0)
            {
                listaGrupos = pControladorProyectoMVC.ObtenerGruposPorID(mCargadorResultadosModel.IdentidadActual.ListaGruposIdentidadParticipacion).Values.ToList();
            }

            return listaGrupos;
        }

        [NonAction]
        protected CategoryModel CargarCategoria(CategoriaTesauro pCategoria)
        {
            CategoryModel categoriaTesauro = new CategoryModel();
            categoriaTesauro.Key = pCategoria.Clave;
            categoriaTesauro.Name = pCategoria.FilaCategoria.Nombre;
            categoriaTesauro.Order = pCategoria.FilaCategoria.Orden;

            categoriaTesauro.ParentCategoryKey = Guid.Empty;
            if (pCategoria.Padre is CategoriaTesauro)
            {
                if (pCategoria.FilaAgregacion != null)
                {
                    categoriaTesauro.Order = pCategoria.FilaAgregacion.Orden;
                    categoriaTesauro.ParentCategoryKey = ((CategoriaTesauro)pCategoria.Padre).Clave;
                }
            }

            return categoriaTesauro;
        }
        [NonAction]
        private static string ObtenerPestañaDeParametros(string pParametrosAdicionales)
        {
            string pestaña = pParametrosAdicionales;

            if (pestaña.Contains("|"))
            {
                char[] separadores = { '|' };
                string[] filtros = pestaña.Split(separadores, StringSplitOptions.RemoveEmptyEntries);
                pestaña = "";
                string separador = "";

                foreach (string filtro in filtros)
                {
                    //Si hay algun orden, lo quito de la pestaña
                    if (!filtro.StartsWith("orden=") && !filtro.StartsWith("ordenarPor="))
                    {
                        pestaña += separador + filtro;
                        separador = "|";
                    }
                }
            }

            return pestaña;
        }

        /// <summary>
        /// Ajusta el modelo de los recursos de la búsqueda.
        /// </summary>
        /// <param name="pListaRecursosModel">Lista de recursos de búsqueda</param>
        /// <param name="pTipoBusqueda">Tipo de busqueda actual</param>
        [NonAction]
        private void AjustarModeloRecursos(Dictionary<Guid, ResourceModel> pListaRecursosModel, short pTipoBusqueda)
        {
            if (pTipoBusqueda == (short)TipoBusqueda.EditarRecursosPerfil)
            {
                foreach (ResourceModel model in pListaRecursosModel.Values)
                {
                    model.SelectionCheckAvailable = true;
                }
            }
        }

        /// <summary>
        /// Obtiene el texto para el número de resultados.
        /// </summary>
        /// <returns>Texto para el número de resultados</returns>
        [NonAction]
        private string ObtenerTextoNumResultados()
        {
            //string texto = "<strong>" + mCargadorResultadosModel.NumeroResultados.ToString() + "</strong> ";
            string texto = mCargadorResultadosModel.NumeroResultados.ToString();

            if (mCargadorResultadosModel.TipoBusqueda == TipoBusqueda.Mensajes)
            {
                texto += GetText("LISTARECURSOS", ClaveTextoSegunNumResul("NUMEROMENSAJES", mCargadorResultadosModel.NumeroResultados));
            }
            else if (mCargadorResultadosModel.TipoBusqueda == TipoBusqueda.Comentarios)
            {
                texto += GetText("LISTARECURSOS", ClaveTextoSegunNumResul("NUMEROCOMENT", mCargadorResultadosModel.NumeroResultados));
            }
            else if (mCargadorResultadosModel.TipoBusqueda == TipoBusqueda.Invitaciones)
            {
                texto += GetText("LISTARECURSOS", ClaveTextoSegunNumResul("NUMEROINVT", mCargadorResultadosModel.NumeroResultados));
            }
            else if (mCargadorResultadosModel.TipoBusqueda == TipoBusqueda.Notificaciones)
            {
                texto += GetText("LISTARECURSOS", ClaveTextoSegunNumResul("NUMERONOT", mCargadorResultadosModel.NumeroResultados));
            }
            else if (mCargadorResultadosModel.TipoBusqueda == TipoBusqueda.Suscripciones)
            {
                texto += GetText("LISTARECURSOS", ClaveTextoSegunNumResul("NUMEROSUSCP", mCargadorResultadosModel.NumeroResultados));
            }
            else if (mCargadorResultadosModel.TipoBusqueda == TipoBusqueda.Contactos || mCargadorResultadosModel.TipoBusqueda == TipoBusqueda.Recomendaciones)
            {
                texto += GetText("LISTARECURSOS", ClaveTextoSegunNumResul("NUMEROCONTACT", mCargadorResultadosModel.NumeroResultados));
            }
            else if (mCargadorResultadosModel.TipoBusqueda == TipoBusqueda.Contribuciones)
            {
                texto += GetText("LISTARECURSOS", ClaveTextoSegunNumResul("NUMEROCONTRI", mCargadorResultadosModel.NumeroResultados));
            }
            else if (mCargadorResultadosModel.TipoBusqueda == TipoBusqueda.Comunidades || mCargadorResultadosModel.TipoBusqueda == TipoBusqueda.RecomendacionesProys)
            {
                texto += GetText("LISTARECURSOS", ClaveTextoSegunNumResul("NUMEROCOMUNI", mCargadorResultadosModel.NumeroResultados));
            }
            else
            {
                texto += GetText("LISTARECURSOS", ClaveTextoSegunNumResul("NUMERORESULTADOS", mCargadorResultadosModel.NumeroResultados));
            }

            return texto;
        }

        /// <summary>
        /// Obtiene la clave de texto correcta según el número de resultados.
        /// </summary>
        /// <param name="pClave">Clave base</param>
        /// <param name="pNumResul">Número de resultados</param>
        /// <returns>Clave de texto correcta según el número de resultados</returns>
        [NonAction]
        private string ClaveTextoSegunNumResul(string pClave, int pNumResul)
        {
            if (pNumResul == 1)
            {
                return pClave + "1";
            }
            else
            {
                return pClave;
            }
        }
        [NonAction]
        private void AsignarPropiedadesCargadorResultadosModel()
        {
            if (mCargadorResultadosModel.IdentidadActual == null)
            {
                mCargadorResultadosModel.IdentidadActual = mControladorBase.ObtenerIdentidadUsuarioInvitado(UtilIdiomas).ListaIdentidades[UsuarioAD.Invitado];
            }

            if (string.IsNullOrEmpty(mCargadorResultadosModel.UrlNavegador))
            {
                mCargadorResultadosModel.UrlNavegador = UrlPaginaActual;
            }
        }

        private object InicializarVariablesMetodoDePeticion(string pNombreModelo)
        {
            object objetoPeticion = System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(pNombreModelo);

            List<string> listaVariables = System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(pNombreModelo).GetType().GetProperties().Select(item => item.Name).ToList();

            foreach (string nombreVariable in listaVariables)
            {
                objetoPeticion.GetType().GetProperty(nombreVariable).SetValue(objetoPeticion, Request.Query[nombreVariable]);
            }

            return objetoPeticion;
        }

        [NonAction]
        private List<Guid> ObtenerListaDeExcluidos(string pLista)
        {
            List<Guid> listaExcluidos = new List<Guid>();

            if (!string.IsNullOrEmpty(pLista))
            {
                string[] listaIDs = pLista.Split(',');

                foreach (string id in listaIDs)
                {
                    Guid idGuid;

                    if (!string.IsNullOrEmpty(id) && Guid.TryParse(id, out idGuid))
                    {
                        listaExcluidos.Add(idGuid);
                    }
                }
            }

            return listaExcluidos;
        }
        #endregion


        #endregion

        #region Propiedades

        /// <summary>
        /// Obtiene si se trata de un ecosistema sin metaproyecto
        /// </summary>
        public Guid PersonalizacionEcosistemaID
        {
            get
            {
                if (!mPersonalizacionEcosistemaID.HasValue)
                {
                    mPersonalizacionEcosistemaID = Guid.Empty;
                    //if (mCargadorResultadosModel.ParametrosAplicacionDS.ParametroAplicacion.Select("Parametro = '" + TiposParametrosAplicacion.PersonalizacionEcosistemaID.ToString() + "'").Length > 0)
                    List<ParametroAplicacion> parametrosApliaciones = mCargadorResultadosModel.ParametrosAplicacionDS.Where(parametroApp => parametroApp.Parametro.Equals(TiposParametrosAplicacion.PersonalizacionEcosistemaID.ToString())).ToList();
                    if (parametrosApliaciones.Count > 0)
                    {
                        //mPersonalizacionEcosistemaID = new Guid(mCargadorResultadosModel.ParametrosAplicacionDS.ParametroAplicacion.Select("Parametro = '" + TiposParametrosAplicacion.PersonalizacionEcosistemaID.ToString() + "'")[0]["Valor"].ToString());
                        mPersonalizacionEcosistemaID = new Guid(parametrosApliaciones.FirstOrDefault().Valor);
                    }
                }
                return mPersonalizacionEcosistemaID.Value;
            }
        }

        /// <summary>
        /// Obtiene si se trata de un ecosistema sin metaproyecto
        /// </summary>
        public bool ComunidadExcluidaPersonalizacionEcosistema
        {
            get
            {
                if (!mComunidadExcluidaPersonalizacionEcosistema.HasValue)
                {
                    mComunidadExcluidaPersonalizacionEcosistema = false;

                    //if (mCargadorResultadosModel.ParametrosAplicacionDS.ParametroAplicacion.Select("Parametro = '" + TiposParametrosAplicacion.ComunidadesExcluidaPersonalizacion.ToString() + "'").Length > 0)
                    List<ParametroAplicacion> parametrosAPlicacion = mCargadorResultadosModel.ParametrosAplicacionDS.Where(parametroApp => parametroApp.Parametro.Equals(TiposParametrosAplicacion.ComunidadesExcluidaPersonalizacion.ToString())).ToList();
                    if (parametrosAPlicacion.Count > 0)
                    {
                        //List<string> listaComunidadesExcluidas = new List<string>(mCargadorResultadosModel.ParametrosAplicacionDS.ParametroAplicacion.Select("Parametro = '" + TiposParametrosAplicacion.ComunidadesExcluidaPersonalizacion.ToString() + "'")[0]["Valor"].ToString().ToUpper().Split(','));
                        List<string> listaComunidadesExcluidas = new List<string>(parametrosAPlicacion.FirstOrDefault().Valor.ToString().ToUpper().Split(','));
                        mComunidadExcluidaPersonalizacionEcosistema = listaComunidadesExcluidas.Contains(mCargadorResultadosModel.ProyectoSeleccionado.ToString().ToUpper());
                    }
                }
                return mComunidadExcluidaPersonalizacionEcosistema.Value;
            }
        }

        /// <summary>
        /// Obtiene el idioma para añadirlo a una url (ej: /en)
        /// </summary>
        private string IdiomaPagina
        {
            get
            {
                if (!mCargadorResultadosModel.LanguageCode.Equals("es"))
                {
                    return "/" + mCargadorResultadosModel.LanguageCode;
                }
                return "";
            }
        }

        /// <summary>
        /// Obtiene la url de la página actual
        /// </summary>
        public string UrlPaginaActual
        {
            get
            {
                string url;
                if (Request.Headers.ContainsKey("Referer"))
                {
                    url = Request.Headers["Referer"].ToString();
                }
                else
                {
                    url = $"{mConfigService.ObtenerUrlBase()}{IdiomaPagina}";

                    if (mCargadorResultadosModel.ProyectoSeleccionado.Equals(ProyectoAD.MetaProyecto))
                    {
                        url += mCargadorResultadosModel.UrlPerfil;

                        switch (mCargadorResultadosModel.TipoBusqueda)
                        {
                            case TipoBusqueda.Blogs:
                                url += UtilIdiomas.GetText("URLSEM", "BLOGS");
                                break;
                            case TipoBusqueda.Comunidades:
                                url += UtilIdiomas.GetText("URLSEM", "COMUNIDADES");
                                break;
                            case TipoBusqueda.PersonasYOrganizaciones:
                                url += UtilIdiomas.GetText("URLSEM", "PERSONASYORGANIZACIONES");
                                break;
                            case TipoBusqueda.BusquedaAvanzada:
                                url += UtilIdiomas.GetText("URLSEM", "BUSQUEDAAVANZADA");
                                break;
                        }
                    }
                    else if (mCargadorResultadosModel.Proyecto != null)
                    {
                        if (mCargadorResultadosModel.Proyecto.FilaProyecto.URLPropia != null)
                        {
                            url = mCargadorResultadosModel.Proyecto.UrlPropia(mControladorBase.IdiomaUsuario) + IdiomaPagina;
                        }
                        if (!mCargadorResultadosModel.ParametroProyecto.ContainsKey(ParametroAD.ProyectoSinNombreCortoEnURL) || !mCargadorResultadosModel.ParametroProyecto[ParametroAD.ProyectoSinNombreCortoEnURL].Equals("1"))
                        {
                            url += "/" + UtilIdiomas.GetText("URLSEM", "COMUNIDAD") + "/" + mCargadorResultadosModel.Proyecto.NombreCorto + "/";
                        }
                        else
                        {
                            url += "/";
                        }

                        switch (mCargadorResultadosModel.TipoBusqueda)
                        {
                            case TipoBusqueda.Recursos:
                                url += UtilIdiomas.GetText("URLSEM", "RECURSOS");
                                break;
                            case TipoBusqueda.Preguntas:
                                url += UtilIdiomas.GetText("URLSEM", "PREGUNTAS");
                                break;
                            case TipoBusqueda.Encuestas:
                                url += UtilIdiomas.GetText("URLSEM", "ENCUESTAS");
                                break;
                            case TipoBusqueda.PersonasYOrganizaciones:
                                url += UtilIdiomas.GetText("URLSEM", "PERSONASYORGANIZACIONES");
                                break;
                            case TipoBusqueda.Debates:
                                url += UtilIdiomas.GetText("URLSEM", "DEBATES");
                                break;
                            case TipoBusqueda.Dafos:
                                url += UtilIdiomas.GetText("URLSEM", "DAFOS");
                                break;
                            case TipoBusqueda.BusquedaAvanzada:
                                url += UtilIdiomas.GetText("URLSEM", "BUSQUEDAAVANZADA");
                                break;
                        }
                    }
                }
                return url;
            }
        }

        /// <summary>
        /// Obtiene o establece la información sobre el idioma del usuario
        /// </summary>
        public UtilIdiomas UtilIdiomas
        {
            get
            {
                if (mUtilIdiomas == null)
                {
                    Guid proyectoID;

                    if (mCargadorResultadosModel.Proyecto == null)
                    {
                        proyectoID = ProyectoAD.MetaProyecto;
                    }
                    else
                    {
                        proyectoID = mCargadorResultadosModel.Proyecto.FilaProyecto.ProyectoID;
                    }

                    mUtilIdiomas = new UtilIdiomas(mCargadorResultadosModel.LanguageCode, proyectoID, mCargadorResultadosModel.Proyecto.PersonalizacionID, mControladorBase.PersonalizacionEcosistemaID, mLoggingService, mEntityContext, mConfigService);
                    //Establecemos el CultureInfo
                    CultureInfo cultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
                    switch (UtilIdiomas.LanguageCode)
                    {
                        case "es":
                            cultureInfo = new CultureInfo("es-ES");
                            break;
                        case "en":
                            cultureInfo = new CultureInfo("en-US");
                            break;
                        case "eu":
                            cultureInfo = new CultureInfo("eu-ES");
                            break;
                        case "gl":
                            cultureInfo = new CultureInfo("gl-ES");
                            break;
                        case "ca":
                            cultureInfo = new CultureInfo("ca-ES");
                            break;
                        case "it":
                            cultureInfo = new CultureInfo("it-IT");
                            break;
                        case "pt":
                            cultureInfo = new CultureInfo("pt-PT");
                            break;
                        case "fr":
                            cultureInfo = new CultureInfo("fr-FR");
                            break;
                        case "de":
                            cultureInfo = new CultureInfo("de-DE");
                            break;
                    }
                    Thread.CurrentThread.CurrentCulture = cultureInfo;
                }
                return mUtilIdiomas;
            }
            set
            {
                mUtilIdiomas = value;
            }
        }

        /// <summary>
        /// Obtiene la URL base de la página
        /// </summary>
        public string BaseURL
        {
            get
            {
                if (Request.Headers.ContainsKey("Referer"))
                {
                    string http = Request.Headers["Referer"].ToString().Substring(0, Request.Headers["Referer"].ToString().IndexOf("/") + 2);
                    string url = Request.Headers["Referer"].ToString().Substring(http.Length);
                    mBaseUrl = http + url.Substring(0, url.IndexOf("/")); ;
                }
                else if (mCargadorResultadosModel.Proyecto != null && mCargadorResultadosModel.Proyecto.FilaProyecto.URLPropia != null)
                {
                    mBaseUrl = mCargadorResultadosModel.Proyecto.UrlPropia(mControladorBase.IdiomaUsuario);
                }

                if (mBaseUrl == null || mBaseUrl == "")
                {
                    mBaseUrl = mConfigService.ObtenerUrlBase();
                }
                return mBaseUrl;
            }
        }

        /// <summary>
        /// Obtiene la URL base en el idioma correspondiente
        /// </summary>
        public string BaseURLIdioma
        {
            get
            {
                string baseUrlIdioma = BaseURL;
                if (UtilIdiomas.LanguageCode != "es")
                {
                    baseUrlIdioma += "/" + UtilIdiomas.LanguageCode;
                }
                return baseUrlIdioma;
            }
        }

        /// <summary>
        /// Obtiene la URL base de la página
        /// </summary>
        public string BaseURLStatic
        {
            get
            {
                if (mBaseURLStatic == null || mBaseURLStatic == "")
                {
                    mBaseURLStatic = mConfigService.ObtenerUrlServicio("urlStatic");

                    if (mBaseURLStatic == null || mBaseURLStatic == "")
                    {
                        mBaseURLStatic = BaseURL;
                    }
                }
                return mBaseURLStatic;
            }
        }

        /// <summary>
        /// Obtiene o establece la URL del los elementos no estaticos de la página
        /// </summary>
        public List<string> BaseURLsContent
        {
            get
            {
                if (mBaseURLsContent == null || mBaseURLsContent.Count == 0)
                {
                    mBaseURLsContent = new List<string>();

                    string urlContent = mConfigService.ObtenerUrlContent();
                    if (!string.IsNullOrEmpty(urlContent))
                    {
                        foreach (string url in urlContent.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (!mBaseURLsContent.Contains(url))
                            {
                                mBaseURLsContent.Add(url);
                            }
                        }
                    }

                    if (mBaseURLsContent == null || mBaseURLsContent.Count == 0)
                    {
                        mBaseURLsContent.Add(BaseURL);
                    }
                }
                return mBaseURLsContent;
            }
            set
            {
                mBaseURLsContent = value;
            }
        }

        #endregion

    }
}
