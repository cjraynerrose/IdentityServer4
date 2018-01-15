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

            Console.WriteLine("User Identity: {0}", context.User.Identity);
            Console.WriteLine("Role is 'Administrator'? : {0}", context.User.IsInRole("Administrator"));
            Console.WriteLine("Identities of user:-");
            foreach (var v in context.User.Identities)
            {
                Console.WriteLine("\tName: {0},\tActor: {1},\tAuthType: {2},\tIsAuth: {3}", v.Name, v.Actor, v.AuthenticationType, v.IsAuthenticated);

                Console.WriteLine("\n\tClaims from Identity:-");
                foreach (var c in v.Claims)
                    Console.WriteLine("\t\tType: {0},\tValue: {1},\tSubject: {2},\tIssuer: {3}", c.Type, c.Value, c.Subject, c.Issuer);
            }

            Console.WriteLine("Claims from other source:-");

            foreach(Claim c in context.User.Claims)
            {
                Console.WriteLine("\t\tType: {0},\tValue: {1},\tSubject: {2},\tIssuer: {3}", c.Type, c.Value, c.Subject, c.Issuer);
            }

            Console.WriteLine("\n *** Starting Authroization. ***\n");

            Claim
                role = context.User.FindFirst("role"),
                accessLevel = context.User.FindFirst("AdminPermission");


            if (role == null)
                Console.WriteLine("\tUser as no 'role' : '{0}'", role == null ? "null" : role.Value);
            else
                Console.WriteLine("\tUser has 'role' : '{0}'", role.Value);

            if (accessLevel == null)
                Console.WriteLine("\tUser has no claim 'AdminPermission' : '{0}'", accessLevel == null ? "null" : accessLevel.Value);
            else
                Console.WriteLine("\tUser has 'AdminPermission' : '{0}'", accessLevel.Value);

            if (role != null && accessLevel != null)
            {
                if (role.Value == "Administrator")
                    context.Succeed(requirement);
            }
            else
                Console.WriteLine("\n *** Authorization Failue. ***\n");



            return Task.CompletedTask;
        }

    }
}
