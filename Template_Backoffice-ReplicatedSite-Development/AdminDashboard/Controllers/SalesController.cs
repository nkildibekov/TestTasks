using ExigoService;
using AdminDashboard.Models;
using AdminDashboard.Services;
using AdminDashboard.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;
using Common;
using Dapper;


namespace AdminDashboard.Controllers
{
    public class SalesController : Controller
    {
        public ActionResult Index()
        {
            if (Request.IsAjaxRequest()) return PartialView();
            else return View();
        }

        public ActionResult SalesByWarehouseCurrency()
        {
            var model = new ProductSalesViewModel();
            using (var context = Exigo.Sql())
            {
                model.ProductSales = context.Query<ProductSalesRecord>(@"
                    SELECT o.WarehouseID
	                        , o.CurrencyCode
	                        , w.WarehouseDescription
	                        , od.ItemCode
	                        , od.ItemDescription
	                        , Total = sum(o.SubTotal)
                    FROM
	                    Orders o
	                    INNER JOIN OrderDetails od
		                    ON od.OrderID = o.OrderID
	                    INNER JOIN Items i
		                    ON i.ItemID = od.ItemID
	                    INNER JOIN Warehouses w
		                    ON w.WarehouseID = o.WarehouseID

                    WHERE 
                        o.OrderDate >= @startdate
                        AND o.OrderDate < @enddate

                    GROUP BY
	                    o.WarehouseID
                        , w.WarehouseDescription
                        , o.CurrencyCode
                        , od.ItemCode
                        , od.ItemDescription

                    ORDER BY
	                    o.WarehouseID
                        , sum(o.SubTotal) DESC
                ", new
                 {
                     startdate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                     enddate = new DateTime(DateTime.Now.AddMonths(1).Year, DateTime.Now.AddMonths(1).Month, 1)
                 }).ToList();
            }

            if (Request.IsAjaxRequest()) return PartialView(model);
            else return View(model);
        }

        public ActionResult SalesByItem()
        {
            if (Request.IsAjaxRequest()) return PartialView();
            else return View();
        }
        [HttpPost]
        public ActionResult SalesByItem(DateTime date)
        {
            if (!GlobalUtilities.VerifySqlTableExists(GlobalSettings.Exigo.Api.Sql.ConnectionStrings.SqlReporting, "Custom_DailyItemSalesbyCountry"))
            {
                return new JsonNetResult(new
                {
                    success = false,
                    error = "Report not configured"
                });
            }


            ViewBag.Date = date;
            ViewBag.BeginningOfMonthDate = new DateTime(date.Year, date.Month, 1);
            ViewBag.BeginningOfYearDate = new DateTime(date.Year, 1, 1);


            var webCategoryItems = new List<WebCategoryItem>();
            var itemSales = new List<ItemSalesRecord>();
            var itemRefunds = new List<ItemSalesRecord>();

            using (var context = Exigo.Sql())
            {
                var data = context.QueryMultiple(@"
                    set transaction isolation level read uncommitted

                    -- Categories
                    select 
	                    Category = wc.WebCategoryDescription,
	                    i.ItemCode
                    from WebCategoryItems wci
                    inner join Items i
	                    on i.ItemID = wci.ItemID
                    inner join WebCategories wc
	                    on wc.WebCategoryID = wci.WebCategoryID
	                    and wc.WebID = wci.WebID
                    where
	                    wci.WebID = 1
	                    and wc.ParentID = 77
                    order by 
	                    wc.SortOrder,
	                    wci.SortOrder


                    -- Sales
                    Select 
	                    i.ItemDescription           AS ItemDescription, 
	                    i.ItemCode AS ItemCode,
	                    --country,
	                    COALESCE(QuantityPerDay, 0) as QuantityPerDay,
	                    COALESCE(AmountPerDay, 0) as AmountPerDay,
	                    COALESCE(TaxPerDay, 0) as TaxPerDay,
	                    COALESCE(TotalPerDay, 0) as TotalPerDay,

	                    COALESCE(QuantityPerMonth, 0) as QuantityPerMonth,
	                    COALESCE(AmountPerMonth, 0) as AmountPerMonth,
	                    COALESCE(TaxPerMonth, 0) as TaxPerMonth,
	                    COALESCE(TotalPerMonth, 0) as TotalPerMonth,

	                    COALESCE(QuantityPerYear, 0) as QuantityPerYear,
	                    COALESCE(AmountPerYear, 0) as AmountPerYear,
	                    COALESCE(TaxPerYear, 0) as TaxPerYear,
	                    COALESCE(TotalPerYear, 0) as TotalPerYear,

	                    COALESCE(QuantityPerLastYear, 0) as QuantityPerLastYear,
	                    COALESCE(AmountPerLastYear, 0) as AmountPerLastYear,
	                    COALESCE(TaxPerLastYear, 0) as TaxPerLastYear,
	                    COALESCE(TotalPerLastYear, 0) as TotalPerLastYear

                    from items i
                    inner join 
                    (select itemid,
		                    --country,
		                    sum(case when SalesDate  = @Date then Quantity   else 0 end) as Quantityperday,
		                    sum(case when SalesDate = @Date then pricetotal  else 0 end) as amountperday,
		                    sum(case when SalesDate = @Date then TaxTotal  else 0 end) as Taxperday,
		                    sum(case when SalesDate = @Date then TaxTotal + PriceTotal else 0 end) as Totalperday,

		                    sum(case when month(SalesDate)  = month(@Date) then Quantity   else 0 end) as QuantityperMonth,
		                    sum(case when month(SalesDate) = month(@Date) then pricetotal  else 0 end) as amountperMonth,
		                    sum(case when month(SalesDate) = month(@Date) then TaxTotal  else 0 end) as TaxperMonth,
		                    sum(case when month(SalesDate) = month(@Date) then TaxTotal + PriceTotal else 0 end) as TotalperMonth,

		                    sum(case when year(SalesDate)  = year(@Date) then Quantity   else 0 end) as QuantityperYear,
		                    sum(case when year(SalesDate) = year(@Date) then pricetotal  else 0 end) as amountperYear,
		                    sum(case when year(SalesDate) = year(@Date) then TaxTotal  else 0 end) as TaxperYear,
		                    sum(case when year(SalesDate) = year(@Date) then TaxTotal + PriceTotal else 0 end) as TotalperYear,

		                    sum(case when year(SalesDate)  = year(@Date) - 1 then Quantity   else 0 end) as QuantityperLastYear,
		                    sum(case when year(SalesDate) = year(@Date) - 1 then pricetotal  else 0 end) as amountperLastYear,
		                    sum(case when year(SalesDate) = year(@Date) - 1 then TaxTotal  else 0 end) as TaxperLastYear,
		                    sum(case when year(SalesDate) = year(@Date) - 1 then TaxTotal + PriceTotal else 0 end) as TotalperLastYear

                           from Custom_DailyItemSalesbyCountry
                           where year(SalesDate) = year(@date)
                              and Isreturn = 0
                              --and country = 'US'
                    group by itemid
                    --, country 
                    ) d on d.itemid = i.itemid

                    order by i.ItemDescription


                    -- Returns
                    Select 
	                    i.ItemDescription           AS ItemDescription, 
	                    i.ItemCode AS ItemCode,
	                    --country,
	                    COALESCE(QuantityPerDay, 0) as QuantityPerDay,
	                    COALESCE(AmountPerDay, 0) as AmountPerDay,
	                    COALESCE(TaxPerDay, 0) as TaxPerDay,
	                    COALESCE(TotalPerDay, 0) as TotalPerDay,

	                    COALESCE(QuantityPerMonth, 0) as QuantityPerMonth,
	                    COALESCE(AmountPerMonth, 0) as AmountPerMonth,
	                    COALESCE(TaxPerMonth, 0) as TaxPerMonth,
	                    COALESCE(TotalPerMonth, 0) as TotalPerMonth,

	                    COALESCE(QuantityPerYear, 0) as QuantityPerYear,
	                    COALESCE(AmountPerYear, 0) as AmountPerYear,
	                    COALESCE(TaxPerYear, 0) as TaxPerYear,
	                    COALESCE(TotalPerYear, 0) as TotalPerYear,

	                    COALESCE(QuantityPerLastYear, 0) as QuantityPerLastYear,
	                    COALESCE(AmountPerLastYear, 0) as AmountPerLastYear,
	                    COALESCE(TaxPerLastYear, 0) as TaxPerLastYear,
	                    COALESCE(TotalPerLastYear, 0) as TotalPerLastYear

                    from items i
                    inner join 
                    (select itemid,
		                    --country,
		                    sum(case when SalesDate  = @Date then Quantity   else 0 end) as Quantityperday,
		                    sum(case when SalesDate = @Date then pricetotal  else 0 end) as amountperday,
		                    sum(case when SalesDate = @Date then TaxTotal  else 0 end) as Taxperday,
		                    sum(case when SalesDate = @Date then TaxTotal + PriceTotal else 0 end) as Totalperday,

		                    sum(case when month(SalesDate)  = month(@Date) then Quantity   else 0 end) as QuantityperMonth,
		                    sum(case when month(SalesDate) = month(@Date) then pricetotal  else 0 end) as amountperMonth,
		                    sum(case when month(SalesDate) = month(@Date) then TaxTotal  else 0 end) as TaxperMonth,
		                    sum(case when month(SalesDate) = month(@Date) then TaxTotal + PriceTotal else 0 end) as TotalperMonth,

		                    sum(case when year(SalesDate)  = year(@Date) then Quantity   else 0 end) as QuantityperYear,
		                    sum(case when year(SalesDate) = year(@Date) then pricetotal  else 0 end) as amountperYear,
		                    sum(case when year(SalesDate) = year(@Date) then TaxTotal  else 0 end) as TaxperYear,
		                    sum(case when year(SalesDate) = year(@Date) then TaxTotal + PriceTotal else 0 end) as TotalperYear,

		                    sum(case when year(SalesDate)  = year(@Date) - 1 then Quantity   else 0 end) as QuantityperLastYear,
		                    sum(case when year(SalesDate) = year(@Date) - 1 then pricetotal  else 0 end) as amountperLastYear,
		                    sum(case when year(SalesDate) = year(@Date) - 1 then TaxTotal  else 0 end) as TaxperLastYear,
		                    sum(case when year(SalesDate) = year(@Date) - 1 then TaxTotal + PriceTotal else 0 end) as TotalperLastYear

                           from Custom_DailyItemSalesbyCountry
                           where year(SalesDate) = year(@date)
                              and Isreturn = 1
                              --and country = 'US'
                    group by itemid
                    --, country 
                    ) d on d.itemid = i.itemid

                    order by i.ItemDescription
                ", new
                 {
                     Date = date
                 });

                webCategoryItems = data.Read<WebCategoryItem>().ToList();
                itemSales        = data.Read<ItemSalesRecord>().ToList();
                itemRefunds      = data.Read<ItemSalesRecord>().ToList();
            }


            var model = new List<ItemSalesByCategoryCollection>();

            // Arrange the items by categories
            var distinctWebCategories = webCategoryItems.Select(c => c.Category).Distinct().ToList();
            foreach (var category in distinctWebCategories)
            {
                var collection = new ItemSalesByCategoryCollection();
                collection.Category = category;

                var salesItemCodesInCategory = webCategoryItems.Where(c => c.Category == category).Select(c => c.ItemCode).ToList();
                var salesItemSalesInCategory = itemSales.Where(c => salesItemCodesInCategory.Contains(c.ItemCode)).ToList();
                foreach (var record in salesItemSalesInCategory)
                {
                    collection.Sales.Add(record);
                }

                var refundsItemCodesInCategory = webCategoryItems.Where(c => c.Category == category).Select(c => c.ItemCode).ToList();
                var refundsItemrefundsInCategory = itemRefunds.Where(c => refundsItemCodesInCategory.Contains(c.ItemCode)).ToList();
                foreach (var record in refundsItemrefundsInCategory)
                {
                    collection.Refunds.Add(record);
                }

                model.Add(collection);
            }


            // Create the "All" category
            var masterCollection = new ItemSalesByCategoryCollection();
            masterCollection.Category = "All";

            foreach (var record in itemSales)
            {
                masterCollection.Sales.Add(record);
            }
            foreach (var record in itemRefunds)
            {
                masterCollection.Refunds.Add(record);
            }

            model.Add(masterCollection);



            if (Request.IsAjaxRequest()) return PartialView("_SalesByItemReport", model);
            else return View("_SalesByItemReport", model);
        }

        public ActionResult SalesByCountryItem()
        {
            if (Request.IsAjaxRequest()) return PartialView();
            else return View();
        }
        [HttpPost]
        public ActionResult SalesByCountryItem(DateTime date)
        {
            if (!GlobalUtilities.VerifySqlTableExists(GlobalSettings.Exigo.Api.Sql.ConnectionStrings.SqlReporting, "Custom_DailyItemSalesbyCountry"))
            {
                return new JsonNetResult(new
                {
                    success = false,
                    error = "Report not configured"
                });
            }


            ViewBag.Date = date;
            ViewBag.BeginningOfMonthDate = new DateTime(date.Year, date.Month, 1);
            ViewBag.BeginningOfYearDate = new DateTime(date.Year, 1, 1);


            var webCategoryItems = new List<WebCategoryItem>();
            var itemSales = new List<ItemSalesRecord>();
            var itemRefunds = new List<ItemSalesRecord>();

            using (var context = Exigo.Sql())
            {
                var data = context.QueryMultiple(@"
                    set transaction isolation level read uncommitted


                    -- Categories
                    select 
	                    Category = wc.WebCategoryDescription,
	                    i.ItemCode
                    from WebCategoryItems wci
                    inner join Items i
	                    on i.ItemID = wci.ItemID
                    inner join WebCategories wc
	                    on wc.WebCategoryID = wci.WebCategoryID
	                    and wc.WebID = wci.WebID
                    where
	                    wci.WebID = 1
	                    and wc.ParentID = 77
                    order by 
	                    wc.SortOrder,
	                    wci.SortOrder


                    -- Sales
                    Select 
	                    i.ItemDescription           AS ItemDescription, 
	                    i.ItemCode AS ItemCode,
	                    Country,
	                    COALESCE(QuantityPerDay, 0) as QuantityPerDay,
	                    COALESCE(AmountPerDay, 0) as AmountPerDay,
	                    COALESCE(TaxPerDay, 0) as TaxPerDay,
	                    COALESCE(TotalPerDay, 0) as TotalPerDay,

	                    COALESCE(QuantityPerMonth, 0) as QuantityPerMonth,
	                    COALESCE(AmountPerMonth, 0) as AmountPerMonth,
	                    COALESCE(TaxPerMonth, 0) as TaxPerMonth,
	                    COALESCE(TotalPerMonth, 0) as TotalPerMonth,

	                    COALESCE(QuantityPerYear, 0) as QuantityPerYear,
	                    COALESCE(AmountPerYear, 0) as AmountPerYear,
	                    COALESCE(TaxPerYear, 0) as TaxPerYear,
	                    COALESCE(TotalPerYear, 0) as TotalPerYear,

	                    COALESCE(QuantityPerLastYear, 0) as QuantityPerLastYear,
	                    COALESCE(AmountPerLastYear, 0) as AmountPerLastYear,
	                    COALESCE(TaxPerLastYear, 0) as TaxPerLastYear,
	                    COALESCE(TotalPerLastYear, 0) as TotalPerLastYear

                    from items i
                    inner join 
                    (select itemid,
		                    Country,
		                    sum(case when SalesDate  = @Date then Quantity   else 0 end) as Quantityperday,
		                    sum(case when SalesDate = @Date then pricetotal  else 0 end) as amountperday,
		                    sum(case when SalesDate = @Date then TaxTotal  else 0 end) as Taxperday,
		                    sum(case when SalesDate = @Date then TaxTotal + PriceTotal else 0 end) as Totalperday,

		                    sum(case when month(SalesDate)  = month(@Date) then Quantity   else 0 end) as QuantityperMonth,
		                    sum(case when month(SalesDate) = month(@Date) then pricetotal  else 0 end) as amountperMonth,
		                    sum(case when month(SalesDate) = month(@Date) then TaxTotal  else 0 end) as TaxperMonth,
		                    sum(case when month(SalesDate) = month(@Date) then TaxTotal + PriceTotal else 0 end) as TotalperMonth,

		                    sum(case when year(SalesDate)  = year(@Date) then Quantity   else 0 end) as QuantityperYear,
		                    sum(case when year(SalesDate) = year(@Date) then pricetotal  else 0 end) as amountperYear,
		                    sum(case when year(SalesDate) = year(@Date) then TaxTotal  else 0 end) as TaxperYear,
		                    sum(case when year(SalesDate) = year(@Date) then TaxTotal + PriceTotal else 0 end) as TotalperYear,

		                    sum(case when year(SalesDate)  = year(@Date) - 1 then Quantity   else 0 end) as QuantityperLastYear,
		                    sum(case when year(SalesDate) = year(@Date) - 1 then pricetotal  else 0 end) as amountperLastYear,
		                    sum(case when year(SalesDate) = year(@Date) - 1 then TaxTotal  else 0 end) as TaxperLastYear,
		                    sum(case when year(SalesDate) = year(@Date) - 1 then TaxTotal + PriceTotal else 0 end) as TotalperLastYear

                           from Custom_DailyItemSalesbyCountry
                           where year(SalesDate) = year(@date)
                              and Isreturn = 0
                    group by itemid
                    , Country 
                    ) d on d.itemid = i.itemid

                    order by i.ItemDescription


                    -- Returns
                    Select 
	                    i.ItemDescription           AS ItemDescription, 
	                    i.ItemCode AS ItemCode,
	                    Country,
	                    COALESCE(QuantityPerDay, 0) as QuantityPerDay,
	                    COALESCE(AmountPerDay, 0) as AmountPerDay,
	                    COALESCE(TaxPerDay, 0) as TaxPerDay,
	                    COALESCE(TotalPerDay, 0) as TotalPerDay,

	                    COALESCE(QuantityPerMonth, 0) as QuantityPerMonth,
	                    COALESCE(AmountPerMonth, 0) as AmountPerMonth,
	                    COALESCE(TaxPerMonth, 0) as TaxPerMonth,
	                    COALESCE(TotalPerMonth, 0) as TotalPerMonth,

	                    COALESCE(QuantityPerYear, 0) as QuantityPerYear,
	                    COALESCE(AmountPerYear, 0) as AmountPerYear,
	                    COALESCE(TaxPerYear, 0) as TaxPerYear,
	                    COALESCE(TotalPerYear, 0) as TotalPerYear,

	                    COALESCE(QuantityPerLastYear, 0) as QuantityPerLastYear,
	                    COALESCE(AmountPerLastYear, 0) as AmountPerLastYear,
	                    COALESCE(TaxPerLastYear, 0) as TaxPerLastYear,
	                    COALESCE(TotalPerLastYear, 0) as TotalPerLastYear

                    from items i
                    inner join 
                    (select itemid,
		                    Country,
		                    sum(case when SalesDate  = @Date then Quantity   else 0 end) as Quantityperday,
		                    sum(case when SalesDate = @Date then pricetotal  else 0 end) as amountperday,
		                    sum(case when SalesDate = @Date then TaxTotal  else 0 end) as Taxperday,
		                    sum(case when SalesDate = @Date then TaxTotal + PriceTotal else 0 end) as Totalperday,

		                    sum(case when month(SalesDate)  = month(@Date) then Quantity   else 0 end) as QuantityperMonth,
		                    sum(case when month(SalesDate) = month(@Date) then pricetotal  else 0 end) as amountperMonth,
		                    sum(case when month(SalesDate) = month(@Date) then TaxTotal  else 0 end) as TaxperMonth,
		                    sum(case when month(SalesDate) = month(@Date) then TaxTotal + PriceTotal else 0 end) as TotalperMonth,

		                    sum(case when year(SalesDate)  = year(@Date) then Quantity   else 0 end) as QuantityperYear,
		                    sum(case when year(SalesDate) = year(@Date) then pricetotal  else 0 end) as amountperYear,
		                    sum(case when year(SalesDate) = year(@Date) then TaxTotal  else 0 end) as TaxperYear,
		                    sum(case when year(SalesDate) = year(@Date) then TaxTotal + PriceTotal else 0 end) as TotalperYear,

		                    sum(case when year(SalesDate)  = year(@Date) - 1 then Quantity   else 0 end) as QuantityperLastYear,
		                    sum(case when year(SalesDate) = year(@Date) - 1 then pricetotal  else 0 end) as amountperLastYear,
		                    sum(case when year(SalesDate) = year(@Date) - 1 then TaxTotal  else 0 end) as TaxperLastYear,
		                    sum(case when year(SalesDate) = year(@Date) - 1 then TaxTotal + PriceTotal else 0 end) as TotalperLastYear

                           from Custom_DailyItemSalesbyCountry
                           where year(SalesDate) = year(@date)
                              and Isreturn = 1
                    group by itemid
                    , Country
                    ) d on d.itemid = i.itemid

                    order by i.ItemDescription
                ", new
                 {
                     Date = date
                 });

                webCategoryItems = data.Read<WebCategoryItem>().ToList();
                itemSales = data.Read<ItemSalesRecord>().ToList();
                itemRefunds = data.Read<ItemSalesRecord>().ToList();
            }


            // Create the model
            var model = new List<ItemSalesByCountryCollection>();

            // Arrange the items by country and category
            var countries = new List<string>();
            countries.AddRange(itemSales.Select(c => c.Country).ToList());
            countries.AddRange(itemRefunds.Select(c => c.Country).ToList());
            countries = countries.Distinct().ToList();

            foreach (var country in countries)
            {
                var salesInCountry = itemSales.Where(c => c.Country == country).ToList();
                var refundsInCountry = itemRefunds.Where(c => c.Country == country).ToList();

                // Create the country collection
                var countryItemsCollection = new ItemSalesByCountryCollection();
                countryItemsCollection.Country = country;


                // Arrange the items by categories
                var distinctWebCategories = webCategoryItems.Select(c => c.Category).Distinct().ToList();
                foreach (var category in distinctWebCategories)
                {
                    var collection = new ItemSalesByCategoryCollection();
                    collection.Category = category;

                    var salesItemCodesInCategory = webCategoryItems.Where(c => c.Category == category).Select(c => c.ItemCode).ToList();
                    var salesItemSalesInCategory = salesInCountry.Where(c => salesItemCodesInCategory.Contains(c.ItemCode)).ToList();
                    foreach (var record in salesItemSalesInCategory)
                    {
                        collection.Sales.Add(record);
                    }

                    var refundsItemCodesInCategory = webCategoryItems.Where(c => c.Category == category).Select(c => c.ItemCode).ToList();
                    var refundsItemrefundsInCategory = refundsInCountry.Where(c => refundsItemCodesInCategory.Contains(c.ItemCode)).ToList();
                    foreach (var record in refundsItemrefundsInCategory)
                    {
                        collection.Refunds.Add(record);
                    }

                    countryItemsCollection.Categories.Add(collection);
                }


                // Create the "All" category
                var masterCollection = new ItemSalesByCategoryCollection();
                masterCollection.Category = "All";

                foreach (var record in salesInCountry)
                {
                    masterCollection.Sales.Add(record);
                }
                foreach (var record in refundsInCountry)
                {
                    masterCollection.Refunds.Add(record);
                }
                countryItemsCollection.Categories.Add(masterCollection);

                model.Add(countryItemsCollection);
            }



            if (Request.IsAjaxRequest()) return PartialView("_SalesByCountryItemReport", model);
            else return View("_SalesByCountryItemReport", model);
        }
    }
}