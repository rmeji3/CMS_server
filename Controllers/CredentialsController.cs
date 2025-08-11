using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CMS.Models;
using CMS.Data;
using System.Diagnostics;

namespace CMS.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CredentialsController : ControllerBase
    {
        private readonly ApiContext _context;

        public CredentialsController(ApiContext context)
        {
            _context = context;
        }

        // Create/Edit
        [HttpPost]
        public JsonResult CreateEdit(Credentials credential)
        {
            if (credential.Id == 0) 
                {
                    _context.Credentials.Add(credential);
                } else
                {
                    var credentialInDb = _context.Credentials.Find(credential.Id);

                    if (credentialInDb != null) 
                        return new JsonResult(NotFound());
                    credentialInDb = credential;
                }
                _context.SaveChanges();
                return new JsonResult(Ok(credential));
        }

        // Get
        [HttpGet]
        public JsonResult Get(int id)
        {
            var result = _context.Credentials.Find(id);

            if (result == null)
                return new JsonResult(NotFound());

            return new JsonResult(Ok(result));
        }

        // Delete
        [HttpDelete]
        public JsonResult Delete(int id)
        {
            var result = _context.Credentials.Find(id);
            if (result == null)
                return new JsonResult(NotFound());

            _context.Credentials.Remove(result);
            _context.SaveChanges();

            return new JsonResult(NoContent());
        }
        // Get all
        [HttpGet()]
        public JsonResult GetAll()
        {
            var result = _context.Credentials.ToList();

            return new JsonResult(Ok(result));
        }
    }

}
