using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.EncapsuladoDatos;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.EntityModel.Models.VistaVirtualDS;
using Es.Riam.Gnoss.AD.EntityModelBASE;
using Es.Riam.Gnoss.AD.Facetado.Model;
using Es.Riam.Gnoss.AD.ParametroAplicacion;
using Es.Riam.Gnoss.AD.ServiciosGenerales;
using Es.Riam.Gnoss.AD.Usuarios;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.CL.ParametrosProyecto;
using Es.Riam.Gnoss.Elementos.Identidad;
using Es.Riam.Gnoss.Logica.Facetado;
using Es.Riam.Gnoss.Logica.Identidad;
using Es.Riam.Gnoss.Recursos;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.UtilServiciosWeb;
using Es.Riam.Gnoss.Web.MVC.Controles.Controladores;
using Es.Riam.Gnoss.Web.MVC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace ServicioCargaResultados
{
    [ApiController]
    [Route("[controller]")]
    public class CargadorContextoMensajesController : ControllerBase
    {
        #region Variables buscador facetado

        private string mBaseUrl = "";
        private string mBaseURLStatic = "";
        private List<string> mBaseURLsContent;
        private CargadorResultadosModel mCargadorResultadosModel;
        private UtilIdiomas mUtilIdiomas;

        /// <summary>
        /// Obtiene si el ecosistema tiene una personalizacion de vistas
        /// </summary>
        private Guid? mPersonalizacionEcosistemaID = null;
        private bool? mComunidadExcluidaPersonalizacionEcosistema = null;

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
        private IServicesUtilVirtuosoAndReplication mServicesUtilVirtuosoAndReplication;

        #endregion

        #region Constructor

        public CargadorContextoMensajesController(EntityContext entityContext, LoggingService loggingService, RedisCacheWrapper redisCacheWrapper, ConfigService configService, VirtuosoAD virtuosoAD, GnossCache gnossCache, UtilServicios utilServicios, IHttpContextAccessor httpContextAccessor, EntityContextBASE entityContextBASE, ICompositeViewEngine viewEngine, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication):
            base(loggingService,configService, entityContext, redisCacheWrapper, gnossCache, virtuosoAD, httpContextAccessor, servicesUtilVirtuosoAndReplication)
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
            mServicesUtilVirtuosoAndReplication = servicesUtilVirtuosoAndReplication;
            mCargadorResultadosModel = new CargadorResultadosModel(entityContext, loggingService, redisCacheWrapper, configService, virtuosoAD, mServicesUtilVirtuosoAndReplication);
        }

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
                    List<ParametroAplicacion> parametrosAplicacion = mCargadorResultadosModel.ParametrosAplicacionDS.Where(parametroApp => parametroApp.Parametro.Equals(TiposParametrosAplicacion.PersonalizacionEcosistemaID.ToString())).ToList();
                    if (parametrosAplicacion.Count > 0)
                    {
                        mPersonalizacionEcosistemaID = new Guid(parametrosAplicacion[0].Valor.ToString());
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
                    List<ParametroAplicacion> parametrosAplicacion = mCargadorResultadosModel.ParametrosAplicacionDS.Where(parametroApp => parametroApp.Parametro.Equals(TiposParametrosAplicacion.ComunidadesExcluidaPersonalizacion.ToString())).ToList();
                    if (parametrosAplicacion.Count > 0)
                    {
                        List<string> listaComunidadesExcluidas = new List<string>(parametrosAplicacion[0].Valor.ToString().ToUpper().Split(','));

                        mComunidadExcluidaPersonalizacionEcosistema = listaComunidadesExcluidas.Contains(mCargadorResultadosModel.ProyectoSeleccionado.ToString().ToUpper());
                    }
                }
                return mComunidadExcluidaPersonalizacionEcosistema.Value;
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

                    mUtilIdiomas = new UtilIdiomas(mCargadorResultadosModel.LanguageCode, proyectoID, mCargadorResultadosModel.Proyecto.PersonalizacionID, PersonalizacionEcosistemaID, mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper);
                    //Establecemos el CultureInfo
                    CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
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
                if (mBaseUrl == null || mBaseUrl == "")
                {
                    mBaseUrl = mConfigService.ObtenerUrlBase();                    
                }
                return mBaseUrl;
            }
        }

        /// <summary>
        /// Obtiene la URL estatica base de la página
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

                    if (mBaseURLsContent == null || mBaseURLsContent.Count == 0)
                    {
                        string urlContent = mConfigService.ObtenerUrlContent();
                        if (!string.IsNullOrEmpty(urlContent))
                        {
                            mBaseURLsContent.Add(urlContent);
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

        #region metodos web

        /// <summary>
        /// carga el contexto de los mensajes relacionados con el mensaje actual
        /// </summary>
        /// <param name="usuarioID">codigo identificador del usuario</param>
        /// <param name="identidadID">codigo identidad usuario</param>
        /// <param name="mensajeId">codigo identificador del mensaje</param>
        /// <param name="languageCode">codigo del lenguaje del mensaje</param>
        [HttpGet, HttpPost]
        [Route("CargarContextoMensajes")]
        public ActionResult CargarContextoMensajes(string usuarioID, string identidadID, string mensajeId, string languageCode, string pParametrosBusqueda)
        {
            try
            {
                string mensajes = MontarContextoMensaje(usuarioID.Replace("\"", ""), identidadID.Replace("\"", ""), mensajeId.Replace("\"", ""), languageCode.Replace("\"", ""), pParametrosBusqueda.Replace("\"", ""));

                string funcionCallBack = HttpContext.Request.Query["callback"];

                string resultado = "";
                using (StringWriter sw = new StringWriter())
                {
                    ViewEngineResult viewResult = mViewEngine.FindView(ControllerContext, ObtenerNombreVista("CargarContextoMensajes"), false);

                    if (viewResult.View == null) throw new Exception("View not found: CargarContextoMensajes");
                    ViewContext viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw, new HtmlHelperOptions());
                    viewResult.View.RenderAsync(viewContext);

                    resultado = sw.GetStringBuilder().ToString();
                }

                HttpContext.Response.ContentType = "text/plain";
                HttpContext.Response.WriteAsync(funcionCallBack + "({\"d\":" + JsonSerializer.Serialize(resultado) + "});");
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
            return new EmptyResult();

        }

        /// <summary>
        /// monta el contexto de los mensajes relacionados con el mensaje actual
        /// </summary>
        /// <param name="usuarioID">codigo identificador del usuario</param>
        /// <param name="pIdentidadID">codigo identidad usuario</param>
        /// <param name="mensajeId">codigo identificador del mensaje</param>
        /// <param name="pLanguageCode">codigo del lenguaje del mensaje</param>
        /// <returns></returns>
        [NonAction]
        private string MontarContextoMensaje(string usuarioID, string pIdentidadID, string mensajeId, string pLanguageCode, string pParametrosBusqueda)
        {
            string resultado = "";
            Guid identidadID = new Guid(pIdentidadID);

            IdentidadCN identidadCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
            DataWrapperIdentidad dataWrapperIdentidad = identidadCN.ObtenerIdentidadPorID(identidadID, true);
            GestionIdentidades gestorIdentidad = new GestionIdentidades(dataWrapperIdentidad, mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication);
            Identidad identidadActual = gestorIdentidad.ListaIdentidades[identidadID];
            mCargadorResultadosModel.IdentidadActual = identidadActual;

            UtilIdiomas utilIdiomas = new UtilIdiomas(pLanguageCode, mLoggingService, mEntityContext, mConfigService, mRedisCacheWrapper);

            //obtenemos los mensajes relacionados con el mensaje actual
            FacetadoCN facetadoCN = new FacetadoCN("acidHome_Master", mUtilServicios.UrlIntragnoss, "", "ColaActualizarVirtuosoHome", mEntityContext, mLoggingService, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication);
            FacetadoDS facetadoDS = facetadoCN.ObtenerMensajesRelacionados(usuarioID, mensajeId, pIdentidadID, 5, identidadActual.NombreCompuesto());
            mCargadorResultadosModel.FacetadoDS = facetadoDS;
            mCargadorResultadosModel.ProyectoSeleccionado = ProyectoAD.MetaProyecto;

            if (mCargadorResultadosModel.ListaIdsResultado == null)
            {
                mCargadorResultadosModel.ListaIdsResultado = new CargadorResultadosModel(mEntityContext, mLoggingService, mRedisCacheWrapper, mConfigService, mVirtuosoAD, mServicesUtilVirtuosoAndReplication).ObtenerListaID(facetadoDS, "MensajesRelacionados", "Mensaje");
            }

            #region Cargamos los resultados
            List<Guid> listaRecursosID = new List<Guid>();
            foreach (string idResultado in mCargadorResultadosModel.ListaIdsResultado.Keys)
            {
                listaRecursosID.Add(new Guid(idResultado));
            }

            ControladorProyectoMVC controladorMVC = new ControladorProyectoMVC(UtilIdiomas, BaseURL, BaseURLsContent, BaseURLStatic, mCargadorResultadosModel.Proyecto, mCargadorResultadosModel.ProyectoOrigenID, mCargadorResultadosModel.FilaParametroGeneral, mCargadorResultadosModel.IdentidadActual, mCargadorResultadosModel.EsBot, mLoggingService, mEntityContext, mConfigService, mHttpContextAccessor, mRedisCacheWrapper, mVirtuosoAD, mGnossCache, mEntityContextBASE, mServicesUtilVirtuosoAndReplication);

            Dictionary<Guid, MessageModel> listaMensajesModel = controladorMVC.ObtenerMensajesPorID(listaRecursosID, "", mCargadorResultadosModel.IdentidadActual);

            #endregion

            List<ObjetoBuscadorModel> ListaResultados = new List<ObjetoBuscadorModel>();
            foreach (MessageModel fichaRecurso in listaMensajesModel.Values)
            {
                ListaResultados.Add(fichaRecurso);
            }

            ViewBag.UtilIdiomas = UtilIdiomas;
            ResultadoModel resultadoModel = new ResultadoModel();
            resultadoModel.ListaResultados = ListaResultados;
            resultadoModel.NumeroResultadosTotal = mCargadorResultadosModel.NumeroResultados;
            resultadoModel.NumeroResultadosPagina = mCargadorResultadosModel.FilasPorPagina;
            resultadoModel.NumeroPaginaActual = mCargadorResultadosModel.PaginaActual;
            resultadoModel.UrlBusqueda = mCargadorResultadosModel.UrlNavegador;
            ViewData.Model = resultadoModel;

            CargarPersonalizacion(ProyectoAD.MetaProyecto);

            resultado = JsonSerializer.Serialize(resultadoModel);
            return resultado;
        }


        #endregion

        [NonAction]
        private void CargarPersonalizacion(Guid pProyectoID)
        {
            CommunityModel comunidad = new CommunityModel();

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

                //TODO Javier esta clase esta en Es.Riam.Gnoss.Web.Mvc.Controles hay que migrarla
                //MyVirtualPathProvider.LimpiarListasRutasVirtuales();
            }

            comunidad.PersonalizacionProyectoID = personalizacionProyecto;

            comunidad.ListaPersonalizaciones = new List<string>();
            comunidad.ListaPersonalizacionesEcosistema = new List<string>();

            if (personalizacionProyecto != Guid.Empty)
            {
                foreach (VistaVirtual filaVistaVirtual in vistaVirtualDW.ListaVistaVirtual.Where(item => item.PersonalizacionID.Equals(personalizacionProyecto.ToString())))
                {
                    comunidad.ListaPersonalizaciones.Add(filaVistaVirtual.TipoPagina);
                }

                ViewBag.Personalizacion = "$$$" + personalizacionProyecto;
            }
            if (PersonalizacionEcosistemaID != Guid.Empty)
            {
                foreach (VistaVirtual filaVistaVirtual in vistaVirtualDW.ListaVistaVirtual.Where(item => item.PersonalizacionID.Equals(PersonalizacionEcosistemaID.ToString())))
                {
                    comunidad.ListaPersonalizacionesEcosistema.Add(filaVistaVirtual.TipoPagina);
                }

                ViewBag.PersonalizacionEcosistema = "$$$" + PersonalizacionEcosistemaID;
            }

            if (pProyectoID != ProyectoAD.MetaProyecto)
            {
                comunidad.Url = new GnossUrlsSemanticas(mLoggingService, mEntityContext, mConfigService, mServicesUtilVirtuosoAndReplication).ObtenerURLComunidad(UtilIdiomas, BaseURLIdioma, mCargadorResultadosModel.Proyecto.NombreCorto);
            }
            else
            {
                comunidad.Url = BaseURLIdioma;
            }

            if (mCargadorResultadosModel != null && mCargadorResultadosModel.Proyecto != null)
            {
                comunidad.ShortName = mCargadorResultadosModel.Proyecto.NombreCorto;
            }

            comunidad.Key = pProyectoID;

            ViewBag.Comunidad = comunidad;

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
            }

            identidad.KeyProfile = mCargadorResultadosModel.IdentidadActual.PerfilID;

            ViewBag.IdentidadActual = identidad;

            string controllerName = this.ToString();
            controllerName = controllerName.Substring(controllerName.LastIndexOf('.') + 1);
            controllerName = controllerName.Substring(0, controllerName.IndexOf("Controller"));
            ViewBag.ControllerName = controllerName;
        }
    }
}
