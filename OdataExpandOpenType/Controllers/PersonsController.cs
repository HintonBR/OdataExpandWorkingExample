using System.Collections.Generic;
using System.Net.Http;
using System.Web.OData.Extensions;
using Microsoft.Data.OData.Query.SemanticAst;

namespace OdataExpandOpenType.Controllers
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Web.Http;
    using System.Web.Http.Description;
    using System.Web.Http.OData.Query;
    using System.Web.OData;
    using System.Web.OData.Routing;

    [EnableQuery]
    public class PersonsController : ODataController
    {
        public PersonsController()
        {
            this.dbContext = new PersonContext();
        }

        private readonly PersonContext dbContext;

        [HttpGet]
        [ResponseType(typeof(IQueryable<Person>))]
        [ODataRoute("Persons")]
        [Route("api/Persons")]
        public IHttpActionResult Get()
        {
            var urlpath = this.Request.GetQueryNameValuePairs()
                .Where(item => item.Key == "$expand");
            var expandSegment = urlpath as IList<KeyValuePair<string, string>> ?? urlpath.ToList();
            if (expandSegment.Count() > 1) throw new Exception($"Only expected to find $expand once in the URL but found {expandSegment.Count()}");
           
            if (!expandSegment.Any())
            {
                this.Request.RequestUri = new Uri(this.Request.RequestUri.AbsoluteUri + (this.Request.GetQueryNameValuePairs().Count() == 0 ? "?" : "&") + "$expand=Attributes");
            }
            else
            {
                var segment = expandSegment.First();
                var expandSegmentAsUrl = expandSegment.Select(item => item.Key + "=" + item.Value).First();
                var childrenExpandsWithAttributes =
                    string.Join(",", segment.Value.Split(',').Select(item => item + "($expand=Attributes)"));
                var newExpandSegmentAsUrl = segment.Key + "=Attributes," + childrenExpandsWithAttributes;
                //TODO:This needs to be smart enough to handle sub expands so probably more like a regex with a begin that is &$expand or ?$expand
                //we also should navigate the children $expands and add attributes
                this.Request.RequestUri = new Uri(this.Request.RequestUri.AbsoluteUri.Replace(expandSegmentAsUrl, newExpandSegmentAsUrl));
            }

            var persons = this.dbContext.Persons;
            
            return this.Ok(persons);
        }

        [HttpPost]
        [ResponseType(typeof(Person))]
        [ODataRoute("Persons")]
        [Route("api/Persons")]
        public async Task<IHttpActionResult> Post([FromBody] OpenPerson openPerson)
        {
            var person = openPerson.ToPerson();
            this.dbContext.Persons.Add(person);
            await this.dbContext.SaveChangesAsync();
            return this.Ok(person);
        }
    }

    public static class PrivateHelper
    {
        /// <summary>
        /// Returns a _private_ Property Value from a given Object. Uses Reflection.
        /// Throws a ArgumentOutOfRangeException if the Property is not found.
        /// </summary>
        /// <typeparam name="T">Type of the Property</typeparam>
        /// <param name="obj">Object from where the Property Value is returned</param>
        /// <param name="propName">Propertyname as string.</param>
        /// <returns>PropertyValue</returns>
        public static T GetPrivatePropertyValue<T>(this object obj, string propName)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            PropertyInfo pi = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (pi == null) throw new ArgumentOutOfRangeException("propName", string.Format("Property {0} was not found in Type {1}", propName, obj.GetType().FullName));
            return (T)pi.GetValue(obj, null);
        }

        /// <summary>
        /// Returns a private Property Value from a given Object. Uses Reflection.
        /// Throws a ArgumentOutOfRangeException if the Property is not found.
        /// </summary>
        /// <typeparam name="T">Type of the Property</typeparam>
        /// <param name="obj">Object from where the Property Value is returned</param>
        /// <param name="propName">Propertyname as string.</param>
        /// <returns>PropertyValue</returns>
        public static T GetPrivateFieldValue<T>(this object obj, string propName)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            Type t = obj.GetType();
            FieldInfo fi = null;
            while (fi == null && t != null)
            {
                fi = t.GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                t = t.BaseType;
            }
            if (fi == null) throw new ArgumentOutOfRangeException("propName", string.Format("Field {0} was not found in Type {1}", propName, obj.GetType().FullName));
            return (T)fi.GetValue(obj);
        }

        /// <summary>
        /// Sets a _private_ Property Value from a given Object. Uses Reflection.
        /// Throws a ArgumentOutOfRangeException if the Property is not found.
        /// </summary>
        /// <typeparam name="T">Type of the Property</typeparam>
        /// <param name="obj">Object from where the Property Value is set</param>
        /// <param name="propName">Propertyname as string.</param>
        /// <param name="val">Value to set.</param>
        /// <returns>PropertyValue</returns>
        public static void SetPrivatePropertyValue<T>(this object obj, string propName, T val)
        {
            Type t = obj.GetType();
            if (t.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) == null)
                throw new ArgumentOutOfRangeException("propName", string.Format("Property {0} was not found in Type {1}", propName, obj.GetType().FullName));
            t.InvokeMember(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance, null, obj, new object[] { val });
        }

        /// <summary>
        /// Set a private Property Value on a given Object. Uses Reflection.
        /// </summary>
        /// <typeparam name="T">Type of the Property</typeparam>
        /// <param name="obj">Object from where the Property Value is returned</param>
        /// <param name="propName">Propertyname as string.</param>
        /// <param name="val">the value to set</param>
        /// <exception cref="ArgumentOutOfRangeException">if the Property is not found</exception>
        public static void SetPrivateFieldValue<T>(this object obj, string propName, T val)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            Type t = obj.GetType();
            FieldInfo fi = null;
            while (fi == null && t != null)
            {
                fi = t.GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                t = t.BaseType;
            }
            if (fi == null) throw new ArgumentOutOfRangeException("propName", string.Format("Field {0} was not found in Type {1}", propName, obj.GetType().FullName));
            fi.SetValue(obj, val);
        }
    }
}
