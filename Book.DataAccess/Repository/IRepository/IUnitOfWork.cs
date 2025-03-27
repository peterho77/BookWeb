using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Book.DataAccess.Repository.IRepository
{
	public interface IUnitOfWork
	{
		ICategoryRepository category { get; }
		IProductRepository product { get; }
		ICompanyRepository company { get; }
		IShoppingCartRepository shoppingCart { get; }
		IApplicationUserRepository applicationUser { get; }
		IOrderDetailRepository orderDetail {  get; }
		IOrderHeaderRepository orderHeader { get; }
        void Save();
	}
}
