﻿using Es.Riam.AbstractsOpen;
using Es.Riam.Gnoss.AD.EntityModel;
using Es.Riam.Gnoss.AD.ParametroAplicacion;
using Es.Riam.Gnoss.AD.Usuarios;
using Es.Riam.Gnoss.AD.Virtuoso;
using Es.Riam.Gnoss.CL;
using Es.Riam.Gnoss.CL.ParametrosAplicacion;
using Es.Riam.Gnoss.Elementos.ParametroAplicacion;
using Es.Riam.Gnoss.Logica.Identidad;
using Es.Riam.Gnoss.Logica.Usuarios;
using Es.Riam.Gnoss.Util.Configuracion;
using Es.Riam.Gnoss.Util.General;
using Es.Riam.Gnoss.Util.Seguridad;
using Es.Riam.Gnoss.Web.Controles;
using Es.Riam.Util;
using Es.Riam.Web.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using static Es.Riam.Web.Util.UtilCookies;

namespace ServicioCargaResultados
{
    public class ControllerBase : Controller
    {
        protected EntityContext mEntityContext;
        protected LoggingService mLoggingService;
        protected VirtuosoAD mVirtuosoAD;
        protected ConfigService mConfigService;
        protected RedisCacheWrapper mRedisCacheWrapper;
        protected GnossCache mGnossCache;
        protected IHttpContextAccessor mHttpContextAccessor;
        protected UtilWeb mUtilWeb;
        protected IServicesUtilVirtuosoAndReplication mServicesUtilVirtuosoAndReplication;

        public ControllerBase(LoggingService loggingService, ConfigService configService, EntityContext entityContext, RedisCacheWrapper redisCacheWrapper, GnossCache gnossCache, VirtuosoAD virtuosoAD, IHttpContextAccessor httpContextAccessor, IServicesUtilVirtuosoAndReplication servicesUtilVirtuosoAndReplication)
        {
            mLoggingService = loggingService;
            mVirtuosoAD = virtuosoAD;
            mConfigService = configService;
            mEntityContext = entityContext;
            mRedisCacheWrapper = redisCacheWrapper;
            mGnossCache = gnossCache;
            mHttpContextAccessor = httpContextAccessor;
            mUtilWeb = new UtilWeb(mHttpContextAccessor);
            mServicesUtilVirtuosoAndReplication = servicesUtilVirtuosoAndReplication;
        }

        [NonAction]
        private bool TienePersonalizacion()
        {
            bool tienePersonalizacion = false;

            if ((!string.IsNullOrEmpty((string)ViewBag.Personalizacion) || !string.IsNullOrEmpty((string)ViewBag.PersonalizacionEcosistema)) && ViewBag.Comunidad != null)
            {
                tienePersonalizacion = true;
            }

            return tienePersonalizacion;
        }
        [NonAction]
        protected internal PartialViewResult PartialView(string viewName)
        {
            if (TienePersonalizacion())
            {
                string nombreVista = viewName;
                nombreVista = nombreVista.Replace("../Shared", "").Trim('/');

                List<string> listaPersonalizaciones = ViewBag.Comunidad.ListaPersonalizaciones;
                List<string> listaPersonalizacionesEcosistema = ViewBag.Comunidad.ListaPersonalizacionesEcosistema;

                if (listaPersonalizaciones.Contains("/Views/" + (string)ViewBag.ControllerName + "/" + nombreVista + ".cshtml") || listaPersonalizaciones.Contains("/Views/" + "Shared" + "/" + nombreVista + ".cshtml"))
                {
                    return base.PartialView(viewName + (string)ViewBag.Personalizacion);
                }
                else if (listaPersonalizacionesEcosistema.Contains("/Views/" + (string)ViewBag.ControllerName + "/" + nombreVista + ".cshtml") || listaPersonalizacionesEcosistema.Contains("/Views/" + "Shared" + "/" + nombreVista + ".cshtml"))
                {
                    return base.PartialView(viewName + (string)ViewBag.PersonalizacionEcosistema);
                }
            }
            return base.PartialView(viewName);
        }
        [NonAction]
        protected internal virtual PartialViewResult PartialView(string viewName, object model)
        {
            if (TienePersonalizacion())
            {
                string nombreVista = viewName;
                nombreVista = nombreVista.Replace("../Shared", "").Trim('/');

                List<string> listaPersonalizaciones = ViewBag.Comunidad.ListaPersonalizaciones;
                List<string> listaPersonalizacionesEcosistema = ViewBag.Comunidad.ListaPersonalizacionesEcosistema;

                if (listaPersonalizaciones.Contains("/Views/" + (string)ViewBag.ControllerName + "/" + nombreVista + ".cshtml") || listaPersonalizaciones.Contains("/Views/" + "Shared" + "/" + nombreVista + ".cshtml"))
                {
                    return base.PartialView(viewName + (string)ViewBag.Personalizacion, model);
                }
                else if (listaPersonalizacionesEcosistema.Contains("/Views/" + (string)ViewBag.ControllerName + "/" + nombreVista + ".cshtml") || listaPersonalizacionesEcosistema.Contains("/Views/" + "Shared" + "/" + nombreVista + ".cshtml"))
                {
                    return base.PartialView(viewName + (string)ViewBag.PersonalizacionEcosistema, model);
                }
            }
            return base.PartialView(viewName, model);
        }
        [NonAction]
        protected internal ViewResult View(string viewName)
        {
            return base.View(ObtenerNombreVista(viewName));
        }
        [NonAction]
        public string ObtenerNombreVista(string viewName)
        {
            if (TienePersonalizacion())
            {
                List<string> listaPersonalizaciones = ViewBag.Comunidad.ListaPersonalizaciones;
                List<string> listaPersonalizacionesEcosistema = ViewBag.Comunidad.ListaPersonalizacionesEcosistema;

                if (listaPersonalizaciones.Contains("/Views/" + (string)ViewBag.ControllerName + "/" + viewName + ".cshtml"))
                {
                    return viewName + (string)ViewBag.Personalizacion;
                }
                else if (listaPersonalizacionesEcosistema.Contains("/Views/" + (string)ViewBag.ControllerName + "/" + viewName + ".cshtml"))
                {
                    return viewName + (string)ViewBag.PersonalizacionEcosistema;
                }
            }
            return viewName;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Guid identidadID = Guid.Parse(Request.Form["pIdentidadID"]);
#if !DEBUG
            if (!identidadID.Equals(UsuarioAD.Invitado))
            {
                try
                {
                    Dictionary<string, string> cookie = UtilCookies.FromLegacyCookieString(Request.Cookies["_UsuarioActual"], mEntityContext);
                    if (cookie != null)
                    {

                        Guid usuarioID = new Guid(cookie["usuarioID"]);
                        IdentidadCN identidadCN = new IdentidadCN(mEntityContext, mLoggingService, mConfigService, mServicesUtilVirtuosoAndReplication);
                        Guid usuarioIDDeBD = identidadCN.ObtenerUsuarioIDConIdentidadID(identidadID);
                        if (!usuarioIDDeBD.Equals(usuarioID))
                        {
                            filterContext.Result = new UnauthorizedResult();
                        }
                    }
                    else
                    {
                        filterContext.Result = new UnauthorizedResult();
                    }
                }
                catch (InvalidCypherTextException)
                {
                    if (mHttpContextAccessor.HttpContext.Request.Cookies.ContainsKey("_UsuarioActual"))
                    {
                        Response.Cookies.Append("_UsuarioActual", Request.Cookies["_UsuarioActual"], new CookieOptions { Expires = new DateTime(2000, 1, 1) });
                    }
                    filterContext.Result = new UnauthorizedResult();
                }
                catch (Exception)
                {
                    filterContext.Result = new UnauthorizedResult();
                }

            }
#endif
        }
    }
}
