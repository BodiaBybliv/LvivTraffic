using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;

namespace LvivTraffic.Models
{
    public class DatabaseDbInitializer : DropCreateDatabaseAlways<ApplicationDbContext>
    {
        protected override void Seed(ApplicationDbContext context)
        {
            var userManager = new ApplicationUserManager(new UserStore<ApplicationUser>(context));
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));

            var role1 = new IdentityRole { Name = "admin" };
            //var role2 = new IdentityRole { Name = "user" };

            roleManager.Create(role1);
            //roleManager.Create(role2);

            var admin = new ApplicationUser { Email = "admin@gmail.com", UserName = "admin@gmail.com" };
            string password = "Admin_admin7";
            var result = userManager.Create(admin, password);
            //var user = new ApplicationUser { Email = "bodia@gmail.com", UserName = "bodia@gmail.com" };
            //password = "B_b123";
            //userManager.Create(user, password);
            if (result.Succeeded)
            {
                userManager.AddToRole(admin.Id, role1.Name);
                //userManager.AddToRole(user.Id, role2.Name);
            }
            string projectDirectory = "D:/StudioProjects/C#/LvivTraffic/";
            var files = new DirectoryInfo(projectDirectory + "Cameras/");
            foreach (FileInfo file in files.GetFiles())
            {
                context.Cameras.Add(new Camera() { DirectoryName = projectDirectory, Name = file.Name, IsAdded = false });
            }
            //context.Markers.Add(new Marker() { Name = "", CarCount = 5, Latitude = "49.834716932080944", Longitude = "24.018583068847636" });
            base.Seed(context);
        }
    }
}