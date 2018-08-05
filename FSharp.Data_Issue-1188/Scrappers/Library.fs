namespace Scrappers
   open FSharp.Data
   open System.Net
   open MinEnvironment
   open System.Collections.Generic
   open System.Threading.Tasks
   open System

   module Aptekaru =

         let baseUrl = "https://apteka.ru/"
         let cookies = CookieContainer()
         let request url_part = 
           Http.RequestString
             ( sprintf "%s%s" baseUrl url_part,
               cookieContainer = cookies )

         request "" |> ignore

         let parse pages query = Task.FromResult(struct (new List<ItemPrice>(), ""))

         let rec parse_search query step (pages: List<string>) (items: List<ItemPrice>) =
           let doc = HtmlDocument.Parse(request (sprintf "search/?PAGEN_products=%i&q=%s" step query))
           pages.Add(doc.Html().InnerText())
           let items_list = doc.CssSelect("div.list.catalog-list article") // always empty, also as "div.items.catalog-items > article", "article", etc.
           items.AddRange
             ( items_list
                 |> Seq.map(fun el -> new ItemPrice (
                      Amount = (match el.TryGetAttribute("data-product-quantity") with 
                                | None -> 0 |> Nullable<int> 
                                | Some attr -> attr.Value() |> int |> Nullable<int>), 
                      Id = (match el.TryGetAttribute("data-product-id") with 
                            | None -> "-" 
                            | Some attr -> attr.Value()), 
                      Name = (match el.TryGetAttribute("data-product-name") with 
                              | None -> "-no name-" 
                              | Some attr -> attr.Value()), 
                      Price = (match el.TryGetAttribute("data-product-price") with 
                               | None -> 0 |> decimal 
                               | Some attr -> attr.Value() |> decimal) )))

           match doc.CssSelect("ul.pagin_items li.arrow_next a").IsEmpty with // always empty
           | false -> parse_search query (step + 1) pages items
           | true -> doc.CssSelect("div.region-select__name").Head.InnerText() // here works ok

         let search_and_parse zoneId query =
           request (sprintf "_action/geoip/setBranch/%s/" zoneId) |> ignore
           let mutable items = new List<ItemPrice>()
           let mutable pages = new List<string>()
           Task.FromResult(struct (pages, items, parse_search query 1 pages items))

     type AptekaruAdapter() =
         interface IAdapter with
             member this.Parse(pages, query) = Aptekaru.parse pages query
             member this.SearchAndParse(zoneId, query) = Aptekaru.search_and_parse zoneId query       