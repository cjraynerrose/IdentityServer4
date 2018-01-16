using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MvcClient.Authorization
{
    public class AdminRequirementHandler : AuthorizationHandler<AdminRequirement>
    {

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
        {
            Console.WriteLine("\n *** Starting Authroization. ***\n");

            Claim role = context.User.FindFirst("role");
            IEnumerable<Claim> accessLevels = context.User.FindAll("adminpermission");

            if (role == null || accessLevels == null || role.Issuer != "http://localhost:5000")
                context.Fail();

            if (role != null && accessLevels != null)
            {
                var claims = accessLevels.Where(c => c.Type == "adminpermission");

                bool readClaim = claims.Any(c => c.Value.Equals("Read", StringComparison.CurrentCultureIgnoreCase));

                if (role.Value == "Administrator" && readClaim)
                    context.Succeed(requirement);
            }
            else
                Console.WriteLine("\n *** Authorization Failue. ***\n");

            return Task.CompletedTask;
        }

    }
}
