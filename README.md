Fling
================

**Fling significantly reduces boilerplate needed to efficiently save/load complex domain entities to/from multiple tables.**

Fling works with your existing (simple, per-table) get/insert/update/delete data access code, and enhances it with minimal boilerplate:

* When loading, Fling fetches child entities in parallel and supports batch loading child entitites for multiple parent entities
* When saving, Fling only inserts/updates/deletes changed rows

Fling is completely database agnostic.

If you use SQL Server, Fling synergizes well with [Facil](https://github.com/cmeeren/Facil), which can provide you with boilerplate-free, type-safe data access code that Fling can use. However, Fling is just as useful without it.

What does it look like?
-----------------------

Given data access code, DTO types and functions to convert between domain and DTO types, Fling allows you to write these three helpers for efficiently saving/loading complex domain entities as described above:

```f#
open Fling.Fling

// 'Order option' is the old order (None for initial insert)
let save : 'arg -> Order option -> Order -> Async<'saveResult option> =
  saveRoot orderToDbDto insertOrder updateOrder
  |> saveChildren
       orderToLineDtos
       (fun dto -> dto.OrderLineId)
       Db.insertOrderLine
       Db.updateOrderLine
       Db.deleteOrderLine
  |> saveOptChild
       orderToCouponDto
       (fun dto -> dto.OrderId)
       Db.insertCoupon
       Db.updateCoupon
       Db.deleteCoupon
  |> saveChild
       Db.orderToPriceDataDto
       Db.insertPriceData
       Db.updatePriceData
       
let load : 'arg -> OrderDto -> Async<Order> =
  createLoader Dto.orderToDomain (fun dto -> dto.OrderId)
  |> loadChild Db.getOrderLinesForOrder
  |> loadChild Db.getCouponForOrder
  |> loadChild Db.getPriceDataForOrder
  |> runLoader
  
let loadBatch : 'arg -> OrderDto list -> Async<Order list> =
  createBatchLoader Dto.orderToDomain (fun dto -> dto.OrderId)
  |> batchLoadChildren Db.getOrderLinesForOrders (fun dto -> dto.OrderId)
  |> batchLoadOptChild Db.getCouponForOrders (fun dto -> dto.OrderId)
  |> batchLoadChild Db.getPriceDataForOrders (fun dto -> dto.OrderId)
  |> runBatchLoader
```

Quick start
-----------

### 1. Install

Install Fling from [NuGet](https://www.nuget.org/packages/Fling).

### 2. Write your domain logic as usual

Below, `Order` is a complex domain object (“aggregate root” in DDD terms) that contains child entities.

```f#
type UserId = UserId of int

type OrderLineId = OrderLineId of int

type OrderId = OrderId of int

type OrderLine =
  { Id: OrderLineId
    ProductName: string }

type Coupon =
  { Code: string
    Expiration: DateTimeOffset }

type PriceData = { NetPrice: decimal }

type Order =
  { Id: OrderId
    OrderNumber: string
    Lines: OrderLine list
    AssociatedUsers: Set<UserId>
    Coupon: Coupon option
    PriceData: PriceData }
```

### 3. Write DTO types corresponding to the database tables

[Facil](https://github.com/cmeeren/Facil) can do this for you.

For demonstration purposes, we store the Order aggregate in five tables: One for the top-level order data, one for the order line data (each order can have 0..N lines), one for the associated users (0..N), one for the coupon used on the order (0..1), and one for the price data (1-to-1).

```f#
type OrderDto = 
  { OrderId: int
    OrderNumber: string }
      
type OrderLineDto =
  { OrderId: int
    OrderLineId: int
    ProductName: string }

type OrderAssociatedUserDto = 
  { OrderId: int
    UserId: int }

type OrderCouponDto =
  { OrderId: int
    Code: string
    Expiration: DateTimeOffset }

type OrderPriceDataDto = 
  { OrderId: int
    NetPrice: decimal }
```

### 4. Write the functions to convert between the domain entities and DTOs

For saving, you need one function for each of the DTO types that accepts the aggregate root (`Order`) and returns the DTO(s).

```f#
let orderToDto (order: Order) : OrderDto =
  failwith "Your code here"

let orderToLineDtos (order: Order) : OrderLineDto list =
  failwith "Your code here"

let orderToAssociatedUserDtos (order: Order) : OrderAssociatedUserDto list =
  failwith "Your code here"

let orderToCouponDto (order: Order) : OrderCouponDto option =
  failwith "Your code here"

let orderToPriceDataDto (order: Order) : OrderPriceDataDto =
  failwith "Your code here"
```

For loading, then you need one function that accepts all relevant DTOs and produce your domain object.

```f#
let orderFromDbDto
  (dto: OrderDto)
  (lines: OrderLineDto list)
  (users: OrderAssociatedUserDto list)
  (coupon: OrderCouponDto option)
  (price: OrderPriceDataDto)
  : Order =
  failwith "Your code here"
```

### 4. Write the individual get/insert/update/delete DB functions for each table

[Facil](https://github.com/cmeeren/Facil) can help you with this.

Note that all of these functions accept `'arg` as their first argument. This can be anything, but will typically be a connection string, a connection object, or tuple containing a connection and a transaction. (Just use `()` if you don’t need it.)

For non-batch loading, you need functions that accept the root ID (the order ID in our case) and return the DTO(s) that belong to the root:

```f#
let getOrderLinesForOrder
  (connStr: string)
  (orderId: int)
  : Async<Dtos.OrderLineDto list> =
  failwith "Your code here"

let getAssociatedUsersForOrder
  (connStr: string)
  (orderId: int)
  : Async<Dtos.OrderAssociatedUserDto list> =
  failwith "Your code here"

let getCouponForOrder
  (connStr: string)
  (orderId: int)
  : Async<Dtos.OrderCouponDto option> =
  failwith "Your code here"

let getPriceDataForOrder
  (connStr: string)
  (orderId: int)
  : Async<Dtos.OrderPriceDataDto> =
  failwith "Your code here"
```

For batch loading, you need functions that accept a list of root IDs and returns all DTOs that belong to those roots:

```f#
let getOrderLinesForOrders
  (connStr: string)
  (orderIds: int list)
  : Async<Dtos.OrderLineDto list> =
  failwith ""

let getAssociatedUsersForOrders
  (connStr: string)
  (orderIds: int list)
  : Async<Dtos.OrderAssociatedUserDto list> =
  failwith ""

let getCouponForOrders
  (connStr: string)
  (orderIds: int list)
  : Async<Dtos.OrderCouponDto list> =
  failwith ""

let getPriceDataForOrders
  (connStr: string)
  (orderIds: int list)
  : Async<Dtos.OrderPriceDataDto list> =
  failwith ""
```

For saving, you need functions to insert/update the root DTO and all (non-optional) to-one child DTOs, and you need functions to insert/update/delete all to-many or optional to-one child DTOs. You typically want to run all of these in a transaction, so for the `'arg` will typically contain a connection/transaction.

You can, if you want, use an “upsert” function instead of insert/update. If you do, just pass this function as both the insert and update function in the next step.

The “insert root” and “update root” functions may return `Async<a>` (e.g. for returning a generated ID), and must both return the same type. All child entity insert/update/delete functions must return `Async<unit>`.

```f#
let insertOrder conn (dto: OrderDto) : Async<unit> =
  failwith "Your code here"

let updateOrder conn (dto: OrderDto) : Async<unit> =
  failwith "Your code here"

let insertOrderLine conn (dto: OrderLineDto) : Async<unit> =
  failwith "Your code here"

let updateOrderLine conn (dto: OrderLineDto) : Async<unit> =
  failwith "Your code here"

let deleteOrderLine conn (orderLineId: int) : Async<unit> =
  failwith "Your code here"

let insertOrderAssociatedUser conn (dto: OrderAssociatedUserDto) : Async<unit> =
  failwith "Your code here"

let updateOrderAssociatedUser conn (dto: OrderAssociatedUserDto) : Async<unit> =
  failwith "Your code here"

let deleteOrderAssociatedUser conn (orderId: int, userId: int) : Async<unit> =
  failwith "Your code here"

let insertCoupon conn (dto: OrderCouponDto) : Async<unit> =
  failwith "Your code here"

let updateCoupon conn (dto: OrderCouponDto) : Async<unit> =
  failwith "Your code here"

let deleteCoupon conn (orderId: int) : Async<unit> =
  failwith "Your code here"

let insertPriceData conn (dto: OrderPriceDataDto) : Async<unit> =
  failwith "Your code here"

let updatePriceData conn (dto: OrderPriceDataDto) : Async<unit> =
  failwith "Your code here"
```

### 5. Wire everything together with Fling

Fling now allows you to wire everything together using a declarative syntax.

#### Helper to load a single root entity with all child entities

Given a single root DTO, the function below loads all child entities in parallel and calls your DTO-to-domain function to return the root entity.

```f#
open Fling.Fling

let load : 'arg -> OrderDto -> Async<Order> =
  createLoader orderFromDto (fun dto -> dto.OrderId)
  |> loadChild getOrderLinesForOrder
  |> loadChild getAssociatedUsersForOrder
  |> loadChild getCouponForOrder
  |> loadChild getPriceDataForOrder
  |> runLoader
```

#### Helper to load multiple root entities with all child entities

Given multiple root DTOs, the function below loads all child entities for all the root entities in parallel and calls your DTO-to-domain function to return the root entities.

In all of the calls below, you specify a function to get the root ID given the child ID. Fling uses this to know which child entities belong to which roots.

```f#
open Fling.Fling

let loadBatch : 'arg -> OrderDto list -> Async<Order list> =
  createBatchLoader orderFromDbDto (fun dto -> dto.OrderId)
  |> batchLoadChildren getOrderLinesForOrders (fun dto -> dto.OrderId)
  |> batchLoadChildren getAssociatedUsersForOrders (fun dto -> dto.OrderId)
  |> batchLoadOptChild getCouponForOrders (fun dto -> dto.OrderId)
  |> batchLoadChild getPriceDataForOrders (fun dto -> dto.OrderId)
  |> runBatchLoader
```

### Helper to save a root entity and all child entities

Given an old root entity (`None` for initial creation, must be `Some` for updates) and an updated root entity, this helper performs the necessary inserts/updates/deletes. It skips updating identical records.

If the root entity was inserted/updated, the function returns `Some` with the result of the insert/update; otherwise it returns `None`.

Everything is done in the order specified here. For to-many child entities, all deletes are performed first, then each new child is either inserted or updated (or skipped if it’s equal).

For to-many and optional to-one children, you specify a function to get the ID (typically the table’s primary key) of the DTO, which will be passed to the `delete` function if the entity needs to be deleted.

```f#
open Fling.Fling

let save : 'arg -> Order option -> Order -> Async<'saveResult option> =
  saveRoot orderToDbDto insertOrder updateOrder
  |> saveChildren
       orderToLineDtos
       (fun dto -> dto.OrderLineId)
       insertOrderLine
       updateOrderLine
       deleteOrderLine
  |> saveChildren
       orderToAssociatedUserDtos
       (fun dto -> dto.OrderId, dto.UserId)
       insertOrderAssociatedUser
       updateOrderAssociatedUser
       deleteOrderAssociatedUser
  |> saveOptChild
       orderToCouponDto
       (fun dto -> dto.OrderId)
       insertCoupon
       updateCoupon
       deleteCoupon
  |> saveChild
       orderToPriceDataDto
       insertPriceData
       updatePriceData
```

Note: You **MUST** pass `Some oldEntity`  if you are updating. Pass `None` only for initial insert of the domain aggregate. If you supply `None` while updating, all child entities will be inserted, which at best will fail with a primary key violation if the entity exists, or at worst will leave old child entities that should have been deleted still present in your database, causing the next load to return invalid data.

### 6. Call the helpers and profit

For example:

```f#
let saveChangesToOrder connStr (oldOrder: Order option) (newOrder: Order) =
  async {
    use conn = new SqlConnection(connStr)
    do! conn.OpenAsync(ct) |> Async.AwaitTask
    use tran = conn.BeginTransaction()
    do! save (conn, tran) oldOrder newOrder
    do! tran.CommitAsync(ct) |> Async.AwaitTask
  }

let getOrderById connStr (OrderId orderId) =
  async {
    match! dbGetOrderById connStr orderId with
    | None -> return None
    | Some orderDto ->
        let! order = load connStr orderDto
        return Some order
  }

let getAllOrders connStr =
  async {
    let! orderDtos = dbGetAllOrders connStr
    return! loadBatch connStr orderDtos
  }
```

## Deployment checklist

For maintainers.

* Make necessary changes to the code
* Update the changelog
* Update the version in `Fling.fsproj`
* Commit and push to `master`. If the GitHub build succeeds, the package is automatically published to NuGet.

