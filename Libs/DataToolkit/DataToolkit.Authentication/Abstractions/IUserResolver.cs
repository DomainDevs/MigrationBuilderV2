using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataToolkit.Authentication.Abstractions;

public interface IUserResolver<TUser>
{
    Task<TUser?> FindAsync(string userId);
}
