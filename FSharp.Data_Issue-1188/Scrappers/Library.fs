namespace Scrappers
   open FSharp.Data
   open System.Net
   open MinEnvironment
   open System.Collections.Generic
   open System.Threading.Tasks
   open System

   module HtmlNode = 
       let tryGetAttributeValue attr = 
           HtmlNode.tryGetAttribute attr
           >> Option.map HtmlAttribute.value 
           
   module Aptekaru =
       let baseUrl = "https://apteka.ru/"
       let search = sprintf "search/?PAGEN_products=%i&q=%s"
       let cookie = CookieContainer()
       let request url_part = 
           let url = sprintf "%s%s" baseUrl url_part 
           Http.RequestString(url, cookieContainer = cookie)

       let setZone id = 
           sprintf "_action/geoip/setBranch/%s/" id
           |> request |> ignore

       let parse pages query = Task.FromResult(struct (new List<ItemPrice>(), ""))

       let nodeToItemPrice el = 
           let amount = 
               HtmlNode.tryGetAttributeValue "data-product-quantity" el
               |> Option.map int
               |> Option.toNullable

           let id = 
               HtmlNode.tryGetAttributeValue "data-product-id" el
               |> Option.defaultValue "-" 

           let name = 
               HtmlNode.tryGetAttributeValue "data-product-name" el
               |> Option.defaultValue "-no name-"

           let price = 
               HtmlNode.tryGetAttributeValue "data-product-price" el
               |> Option.map decimal
               |> Option.defaultValue 0M

           new ItemPrice(Amount = amount, Id = id, Name = name, Price = price)

       let rec parse_search query step (pages: List<string>) (items: List<ItemPrice>) =
           let url = search step query 
           let doc =  url |> request |> HtmlDocument.Parse
           
           pages.Add(doc.Html().InnerText())

           let items_list = 
               doc.CssSelect("div.list.catalog-list article") // always empty, also as "div.items.catalog-items > article", "article", etc.
               |> Seq.map nodeToItemPrice         
           
           items_list |> items.AddRange 

           match doc.CssSelect("ul.pagin_items li.arrow_next a") with // always empty
           | [] -> doc.CssSelect("div.region-select__name").Head.InnerText() // here works ok
           | _ -> parse_search query (step + 1) pages items

       let search_and_parse zoneId query =
           zoneId |> setZone
           let items = new List<ItemPrice>()
           let pages = new List<string>()
           Task.FromResult(struct (pages, items, parse_search query 1 pages items))

   type AptekaruAdapter() =
       interface IAdapter with
           member this.Parse(pages, query) = Aptekaru.parse pages query
           member this.SearchAndParse(zoneId, query) = Aptekaru.search_and_parse zoneId query       