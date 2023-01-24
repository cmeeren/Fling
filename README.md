Fling
================

**Fling significantly reduces boilerplate needed to efficiently save/load complex domain entities to/from multiple
tables.**

Fling works with your existing (simple, per-table) get/insert/update/delete data access code, and enhances it with
minimal boilerplate:

* When loading, Fling fetches child entities and supports batch loading child entities for multiple parent entities.
* When saving, Fling only inserts/updates/deletes changed rows.

Fling is completely database agnostic.

If you use SQL Server, Fling synergizes very well with [Facil](https://github.com/cmeeren/Facil), which can fully
generate the data access code that Fling can use. However, Fling is just as useful without it.

What does it look like?
-----------------------

Given data access code, DTO types and functions to convert between domain and DTO types, Fling allows you to write these
three helpers for efficiently saving/loading complex domain entities as described above:

```f#
open Fling.Fling

// 'Order option' is the old order (None for initial insert)
let save: 'arg -> Order option -> Order -> Async<'saveResult option> =
    saveRoot orderToDbDto insertOrder updateOrder
    |> saveChildren
        orderToLineDtos
        (fun dto -> dto.OrderLineId)
        Db.insertOrderLine
        Db.updateOrderLine
        Db.deleteOrderLine
    |> saveOptChild orderToCouponDto (fun dto -> dto.OrderId) Db.insertCoupon Db.updateCoupon Db.deleteCoupon
    |> saveChild Db.orderToPriceDataDto Db.insertPriceData Db.updatePriceData

let load: 'arg -> OrderDto -> Async<Order> =
    createLoader Dto.orderToDomain (fun dto -> dto.OrderId)
    |> loadChild Db.getOrderLinesForOrder
    |> loadChild Db.getCouponForOrder
    |> loadChild Db.getPriceDataForOrder
    |> loadSerialWithTransaction

let loadBatch: 'arg -> OrderDto list -> Async<Order list> =
    createBatchLoader Dto.orderToDomain (fun dto -> dto.OrderId)
    |> batchLoadChildren Db.getOrderLinesForOrders (fun dto -> dto.OrderId)
    |> batchLoadOptChild Db.getCouponForOrders (fun dto -> dto.OrderId)
    |> batchLoadChild Db.getPriceDataForOrders (fun dto -> dto.OrderId)
    |> loadBatchSerialWithTransaction
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

type OrderLine = { Id: OrderLineId; ProductName: string }

type Coupon = {
    Code: string
    Expiration: DateTimeOffset
}

type PriceData = { NetPrice: decimal }

type Order = {
    Id: OrderId
    OrderNumber: string
    Lines: OrderLine list
    AssociatedUsers: Set<UserId>
    Coupon: Coupon option
    PriceData: PriceData
}
```

### 3. Write DTO types corresponding to the database tables

[Facil](https://github.com/cmeeren/Facil) can generate these for you if you use SQL Server.

For demonstration purposes, we store the Order aggregate in five tables: One for the top-level order data, one for the
order line data (each order can have 0..N lines), one for the associated users (0..N), one for the coupon used on the
order (0..1), and one for the price data (1-to-1).

```f#
type OrderDto = { OrderId: int; OrderNumber: string }

type OrderLineDto = {
    OrderId: int
    OrderLineId: int
    ProductName: string
}

type OrderAssociatedUserDto = { OrderId: int; UserId: int }

type OrderCouponDto = {
    OrderId: int
    Code: string
    Expiration: DateTimeOffset
}

type OrderPriceDataDto = { OrderId: int; NetPrice: decimal }
```

### 4. Write the functions to convert between the domain entities and DTOs

For saving, you need one function for each of the DTO types that accepts the aggregate root (`Order`) and returns the
DTO(s).

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
let orderFromDtos
    (dto: OrderDto)
    (lines: OrderLineDto list)
    (users: OrderAssociatedUserDto list)
    (coupon: OrderCouponDto option)
    (price: OrderPriceDataDto)
    : Order =
    failwith "Your code here"
```

### 4. Write the individual get/insert/update/delete DB functions for each table

[Facil](https://github.com/cmeeren/Facil) can generate these for you if you use SQL Server. If you use Facil, it is
highly recommended you also install Fling.Interop.Facil and
see [the instructions later in the readme](#flinginteropfacil).

Note that all of these functions accept `'arg` as their first argument. This can be anything, but will typically be a
connection string, a connection object, or tuple containing a connection and a transaction. (Just use `()` if you don’t
need it.)

For non-batch loading, you need functions that accept the root ID (the order ID in our case) and return the DTO(s) that
belong to the root:

```f#
let getOrderLinesForOrder (connStr: string) (orderId: int) : Async<Dtos.OrderLineDto list> =
    failwith "Your code here"

let getAssociatedUsersForOrder (connStr: string) (orderId: int) : Async<Dtos.OrderAssociatedUserDto list> =
    failwith "Your code here"

let getCouponForOrder (connStr: string) (orderId: int) : Async<Dtos.OrderCouponDto option> =
    failwith "Your code here"

let getPriceDataForOrder (connStr: string) (orderId: int) : Async<Dtos.OrderPriceDataDto> =
    failwith "Your code here"

```

For batch loading, you need functions that accept a list of root IDs and returns all DTOs that belong to those roots:

```f#
let getOrderLinesForOrders (connStr: string) (orderIds: int list) : Async<Dtos.OrderLineDto list> =
    failwith ""

let getAssociatedUsersForOrders (connStr: string) (orderIds: int list) : Async<Dtos.OrderAssociatedUserDto list> =
    failwith ""

let getCouponForOrders (connStr: string) (orderIds: int list) : Async<Dtos.OrderCouponDto list> =
    failwith ""

let getPriceDataForOrders (connStr: string) (orderIds: int list) : Async<Dtos.OrderPriceDataDto list> =
    failwith ""
```

For saving, you need functions to insert/update the root DTO and all (non-optional) to-one child DTOs, and you need
functions to insert/update/delete all to-many or optional to-one child DTOs. You typically want to run all of these in a
transaction, so for the `'arg` will typically contain a connection/transaction.

You can, if you want, use an “upsert” function instead of insert/update. If you do, just pass this function as both the
insert and update function in the next step.

The “insert root” and “update root” functions may return `Async<'a>` (e.g. for returning a generated ID), and must both
return the same type. All child entity insert/update/delete functions must return `Async<unit>`.

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

Given a single root DTO, the function below loads all child entities in a transaction and calls your DTO-to-domain
function to return the root entity.

```f#
open Fling.Fling

let load: 'arg -> OrderDto -> Async<Order> =
    createLoader orderFromDtos (fun dto -> dto.OrderId)
    |> loadChild getOrderLinesForOrder
    |> loadChild getAssociatedUsersForOrder
    |> loadChild getCouponForOrder
    |> loadChild getPriceDataForOrder
    |> loadSerialWithTransaction
```

#### Helper to load multiple root entities with all child entities

Given multiple root DTOs, the function below loads all child entities for all the root entities and calls your
DTO-to-domain function to return the root entities.

In all of the calls below, you specify a function to get the root ID given the child ID. Fling uses this to know which
child entities belong to which roots.

```f#
open Fling.Fling

let loadBatch: 'arg -> OrderDto list -> Async<Order list> =
    createBatchLoader orderFromDtos (fun dto -> dto.OrderId)
    |> batchLoadChildren getOrderLinesForOrders (fun dto -> dto.OrderId)
    |> batchLoadChildren getAssociatedUsersForOrders (fun dto -> dto.OrderId)
    |> batchLoadOptChild getCouponForOrders (fun dto -> dto.OrderId)
    |> batchLoadChild getPriceDataForOrders (fun dto -> dto.OrderId)
    |> loadBatchSerialWithTransaction
```

#### Helper to save a root entity and all child entities

Given an old root entity (`None` for initial creation, must be `Some` for updates) and an updated root entity, this
helper performs the necessary inserts/updates/deletes. It skips updating identical records (compared using `=`).

Everything is done in the order you specify here. For to-many child entities, all deletes are performed first, then each
new child is either inserted or updated (or skipped if it’s equal).

For to-many and optional to-one children, you specify a function to get the ID (typically the table’s primary key) of
the DTO. This will be passed to the `delete` function if the entity needs to be deleted, and is used for to-many
children to know which child entities to compare, delete, and insert. Though these are trivial, bugs can sneak in
here – [Facil](https://github.com/cmeeren/Facil) can generate these for you if you use SQL Server.

```f#
open Fling.Fling

let save: 'arg -> Order option -> Order -> Async<unit> =
    saveRoot orderToDbDto insertOrder updateOrder
    |> saveChildren orderToLineDtos (fun dto -> dto.OrderLineId) insertOrderLine updateOrderLine deleteOrderLine
    |> saveChildren
        orderToAssociatedUserDtos
        (fun dto -> dto.OrderId, dto.UserId)
        insertOrderAssociatedUser
        updateOrderAssociatedUser
        deleteOrderAssociatedUser
    |> saveOptChild orderToCouponDto (fun dto -> dto.OrderId) insertCoupon updateCoupon deleteCoupon
    |> saveChild orderToPriceDataDto insertPriceData updatePriceData
```

Note: You **MUST** pass `Some oldEntity`  if you are updating. Pass `None` only for initial insert of the domain
aggregate. If you supply `None` while updating, all entities will be inserted, which at best will fail with a primary
key violation if the entity exists, or at worst will leave old child entities that should have been deleted still
present in your database, causing the next load to return invalid data.

If you need to return a result, use `saveRootWithOutput` instead of `saveRoot`. You then get `Async<'a option>` instead
of `Async<unit>`, where `'a` is the return type of your insert and update functions. If the root entity was
inserted/updated, the function returns `Some` with the result of the insert/update; otherwise it returns `None`.

### 6. Call the helpers and profit

For example:

```f#
let saveChangesToOrder connStr (oldOrder: Order option) (newOrder: Order) = async {
    use conn = new SqlConnection(connStr)
    do! conn.OpenAsync() |> Async.AwaitTask
    use tran = conn.BeginTransaction()
    do! save (conn, tran) oldOrder newOrder
    do! tran.CommitAsync() |> Async.AwaitTask
}

let getOrderById connStr (OrderId orderId) = async {
    match! dbGetOrderById connStr orderId with
    | None -> return None
    | Some orderDto ->
        let! order = load connStr orderDto
        return Some order
}

let getAllOrders connStr = async {
    let! orderDtos = dbGetAllOrders connStr
    return! loadBatch connStr orderDtos
}
```

Production readiness
--------------------

Fling is production ready.

Fling is fairly well tested and is used in several mission-critical production services at our company. I’m not claiming
it’s perfect, or even bug-free, but I have a vested interest in keeping it working properly.

It’s still at 0.x because it's still new and I may still be discovering improvements that require breaking changes every
now and then. However, do not take 0.x to mean that it’s a buggy mess, or that the API will radically change every other
week. Breaking changes will cause a lot of churn for me, too.


Fling.Interop.Facil
-------------------

Fling.Interop.Facil
uses [ugly SRTP code](https://github.com/cmeeren/Fling/blob/master/src/Fling.Interop.Facil/Library.fs) to entirely
remove the boilerplate needed to use Fling with the data access code generated by Facil.

Fling.Interop.Facil works with code generated by Facil >= 1.1.0.

To use it, install Fling.Interop.Facil from [NuGet](https://www.nuget.org/packages/Fling.Interop.Facil)
and `open Fling.Interop.Facil.Fling` after `open Fling.Fling`, then use the Facil script/procedure types instead of DB
functions in all Fling functions.

```f#
open Fling.Fling
open Fling.Interop.Facil.Fling
```

For (non-batch) loading:

* Unlike Fling, you have to use `loadChild`, `loadOptChild`, or `loadChildren` depending on the cardinality of the
  relationship (in Fling, `loadChild` serves all three).
* `'arg` is locked to `string`, i.e. a connection string

```f#
open Fling.Fling
open Fling.Interop.Facil.Fling

let load: string -> OrderDto -> Async<Order> =
    createLoader orderFromDtos (fun dto -> dto.OrderId)
    |> loadChildren OrderLine_ByOrderId
    |> loadChildren OrderAssociatedUser_ByOrderId
    |> loadOptChild OrderCoupon_ByOrderId
    |> loadChild OrderPriceData_ByOrderId
    |> loadSerialWithTransaction
```

For batch loading:

* `'arg` is locked to `string`, i.e. a connection string

```f#
let loadBatch: string -> OrderDto list -> Async<Order list> =
    createBatchLoader orderFromDtos (fun dto -> dto.OrderId)
    |> batchLoadChildren OrderLine_ByOrderIds (fun dto -> dto.OrderId)
    |> batchLoadChildren OrderAssociatedUser_ByOrderIds (fun dto -> dto.OrderId)
    |> batchLoadOptChild OrderCoupon_ByOrderIds (fun dto -> dto.OrderId)
    |> batchLoadChild OrderPriceData_ByOrderIds (fun dto -> dto.OrderId)
    |> loadBatchSerialWithTransaction
```

For saving:

* `'arg` is locked to `SqlConnection * SqlTransaction`

```f#
let save: (SqlConnection * SqlTransaction) -> Order option -> Order -> Async<unit> =
    saveRoot orderToDbDto Order_Insert Order_Update
    |> saveChildren
        orderToLineDtos
        DbGen.TableDtos.OrderLine.getPrimaryKey
        OrderLine_Insert
        OrderLine_Update
        OrderLine_Delete
    |> saveChildren
        orderToAssociatedUserDtos
        DbGen.TableDtos.OrderAssociatedUser.getPrimaryKey
        OrderAssociatedUser_Insert
        OrderAssociatedUser_Update
        OrderAssociatedUser_Delete
    |> saveOptChild
        orderToCouponDto
        DbGen.TableDtos.OrderCoupon.getPrimaryKey
        OrderCoupon_Insert
        OrderCoupon_Update
        OrderCoupon_Delete
    |> saveChild orderToPriceDataDto OrderPriceData_Insert OrderPriceData_Update
```

Use `withTransactionFromConnStr` to “convert” the `SqlConnection * SqlTransaction` to a `string` (connection string) and
run the whole save in a transaction. This is useful if you don’t need to run the save in a transaction with anything
else:

```f#
let save : string -> Order option -> Order -> Async<unit> =
    (* same code as above *)
    |> withTransactionFromConnStr
```

As with Fling, use `saveRootWithOutput` instead of `saveRoot` if you need to return anything from the root’s
insert/update script.

Limitations
-----------

### Cannot interleave inserts/updates/deletes for different tables

It’s not possible to interleave inserts/updates/deletes for different tables. For example, you can’t specify that Fling
should *insert* first into table A and then into table B while at the same time  *delete* from table A and then from
table B. The ordering of operations can only be specified at the table (or “child”) level; all inserts/updates/deletes
for a table is performed before the next table. This may have implications for foreign key constraints in complex
aggregates.

### Transactional consistency on load can not be 100% guaranteed

Fling fetches child entities in parallel. I have not found a way to use transactions with parallel commands/connections.
This means that it is not possible to guarantee a transactionally consistent view of the whole aggregate. Specifically,
it is theoretically possible that Facil loads data from some tables, then another operation changes the data, and then
Facil continues to load the now updated data from the rest of the tables. Only you know if this will pose a problem for
you. I have not had problems with it. You may be able to avoid it with locking.

## Deployment checklist

For maintainers.

* Make necessary changes to the code
* Update the changelog
* Update the version in `Fling.fsproj` and/or `Fling.Interop.Facil.fsproj`
* Commit and push to `master`. If the GitHub build succeeds, the packages are automatically published to NuGet.
