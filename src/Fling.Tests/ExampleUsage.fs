module ExampleUsage

open System



module Domain =

    type UserId =
        | UserId of int

        member this.value = let (UserId x) = this in x

    type OrderLineId =
        | OrderLineId of int

        member this.value = let (OrderLineId x) = this in x

    type OrderId =
        | OrderId of int

        member this.value = let (OrderId x) = this in x

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



module Dtos =

    open Domain


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

    type OrderDto = { OrderId: int; OrderNumber: string }


    let orderToLineDtos (o: Order) =
        o.Lines
        |> List.map (fun ol -> {
            OrderId = o.Id.value
            OrderLineId = ol.Id.value
            ProductName = ol.ProductName
        })


    let orderToAssociatedUserDtos (o: Order) =
        o.AssociatedUsers
        |> Set.toList
        |> List.map (fun u -> {
            OrderId = o.Id.value
            UserId = u.value
        })


    let orderToCouponDto (o: Order) =
        o.Coupon
        |> Option.map (fun c -> {
            OrderId = o.Id.value
            Code = c.Code
            Expiration = c.Expiration
        })


    let orderToPriceDataDto (o: Order) = {
        OrderId = o.Id.value
        NetPrice = o.PriceData.NetPrice
    }


    let orderToDbDto (o: Order) = {
        OrderId = o.Id.value
        OrderNumber = o.OrderNumber
    }


    let orderFromDbDto
        (dto: OrderDto)
        (lines: OrderLineDto list)
        (users: OrderAssociatedUserDto list)
        (coupon: OrderCouponDto option)
        (price: OrderPriceDataDto)
        : Order =
        {
            Id = OrderId dto.OrderId
            OrderNumber = dto.OrderNumber
            Lines =
                lines
                |> List.map (fun lineDto -> {
                    Id = OrderLineId lineDto.OrderLineId
                    ProductName = lineDto.ProductName
                })
            AssociatedUsers = users |> List.map (fun x -> UserId x.UserId) |> set
            Coupon =
                coupon
                |> Option.map (fun c -> {
                    Code = c.Code
                    Expiration = c.Expiration
                })
            PriceData = { NetPrice = price.NetPrice }
        }



module DbScripts =


    // Mock types/functions
    type SqlConnection = SqlConnection of _connStr: string

    type SqlTransaction = {
        Dummy: unit
    } with

        interface IDisposable with
            member this.Dispose() = ()

    let createTransaction (_conn: SqlConnection) = { Dummy = () }

    let commit (_tran: SqlTransaction) = ()



    let getAllOrders (_conn: SqlConnection, _tran: SqlTransaction) : Async<Dtos.OrderDto list> = failwith ""

    let getOrderById (_orderId: int) (_conn: SqlConnection, _tran: SqlTransaction) : Async<Dtos.OrderDto option> =
        failwith ""

    let getOrderLinesForOrder
        (_conn: SqlConnection, _tran: SqlTransaction)
        (_orderId: int)
        : Async<Dtos.OrderLineDto list> =
        failwith ""

    let getOrderLinesForOrders
        (_conn: SqlConnection, _tran: SqlTransaction)
        (_orderIds: int list)
        : Async<Dtos.OrderLineDto list> =
        failwith ""

    let getAssociatedUsersForOrder
        (_conn: SqlConnection, _tran: SqlTransaction)
        (_orderId: int)
        : Async<Dtos.OrderAssociatedUserDto list> =
        failwith ""

    let getAssociatedUsersForOrders
        (_conn: SqlConnection, _tran: SqlTransaction)
        (_orderIds: int list)
        : Async<Dtos.OrderAssociatedUserDto list> =
        failwith ""

    let getCouponForOrder
        (_conn: SqlConnection, _tran: SqlTransaction)
        (_orderId: int)
        : Async<Dtos.OrderCouponDto option> =
        failwith ""

    let getCouponForOrders
        (_conn: SqlConnection, _tran: SqlTransaction)
        (_orderIds: int list)
        : Async<Dtos.OrderCouponDto list> =
        failwith ""

    let getPriceDataForOrder
        (_conn: SqlConnection, _tran: SqlTransaction)
        (_orderId: int)
        : Async<Dtos.OrderPriceDataDto> =
        failwith ""

    let getPriceDataForOrders
        (_conn: SqlConnection, _tran: SqlTransaction)
        (_orderIds: int list)
        : Async<Dtos.OrderPriceDataDto list> =
        failwith ""

    let insertOrder (_conn: SqlConnection, _tran: SqlTransaction) (_dto: Dtos.OrderDto) : Async<unit> = async.Return()

    let insertOrders (_conn: SqlConnection, _tran: SqlTransaction) (_dtos: Dtos.OrderDto seq) : Async<unit> =
        async.Return()

    let updateOrder (_conn: SqlConnection, _tran: SqlTransaction) (_dto: Dtos.OrderDto) : Async<unit> = async.Return()

    let updateOrders (_conn: SqlConnection, _tran: SqlTransaction) (_dtos: Dtos.OrderDto seq) : Async<unit> =
        async.Return()

    let insertOrderLine (_conn: SqlConnection, _tran: SqlTransaction) (_dto: Dtos.OrderLineDto) : Async<unit> =
        async.Return()

    let insertOrderLines (_conn: SqlConnection, _tran: SqlTransaction) (_dtos: Dtos.OrderLineDto seq) : Async<unit> =
        async.Return()

    let updateOrderLine (_conn: SqlConnection, _tran: SqlTransaction) (_dto: Dtos.OrderLineDto) : Async<unit> =
        async.Return()

    let updateOrderLines (_conn: SqlConnection, _tran: SqlTransaction) (_dto: Dtos.OrderLineDto seq) : Async<unit> =
        async.Return()

    let deleteOrderLine (_conn: SqlConnection, _tran: SqlTransaction) (_orderLineId: int) : Async<unit> = async.Return()

    let deleteOrderLines (_conn: SqlConnection, _tran: SqlTransaction) (_orderLineIds: int seq) : Async<unit> =
        async.Return()

    let insertOrderAssociatedUser
        (_conn: SqlConnection, _tran: SqlTransaction)
        (_dto: Dtos.OrderAssociatedUserDto)
        : Async<unit> =
        async.Return()

    let insertOrderAssociatedUsers
        (_conn: SqlConnection, _tran: SqlTransaction)
        (_dtos: Dtos.OrderAssociatedUserDto seq)
        : Async<unit> =
        async.Return()

    let updateOrderAssociatedUser
        (_conn: SqlConnection, _tran: SqlTransaction)
        (_dto: Dtos.OrderAssociatedUserDto)
        : Async<unit> =
        async.Return()

    let updateOrderAssociatedUsers
        (_conn: SqlConnection, _tran: SqlTransaction)
        (_dtos: Dtos.OrderAssociatedUserDto seq)
        : Async<unit> =
        async.Return()

    let deleteOrderAssociatedUser
        (_conn: SqlConnection, _tran: SqlTransaction)
        (_orderId: int, _userId: int)
        : Async<unit> =
        async.Return()

    let deleteOrderAssociatedUsers
        (_conn: SqlConnection, _tran: SqlTransaction)
        (_orderUserIds: (int * int) seq)
        : Async<unit> =
        async.Return()

    /// This is for checking that the Fling API works with SRTP matching the expected DTO
    /// shape
    let inline insertCoupon (_conn: SqlConnection, _tran: SqlTransaction) (dto: ^a) : Async<unit> =
        async {
            ignore (^a: (member OrderId: int) dto)
            ignore (^a: (member Code: string) dto)
            ignore (^a: (member Expiration: DateTimeOffset) dto)
        }

    let insertCoupons (_conn: SqlConnection, _tran: SqlTransaction) (_dtos: Dtos.OrderCouponDto seq) : Async<unit> =
        async.Return()

    let updateCoupon (_conn: SqlConnection, _tran: SqlTransaction) (_dto: Dtos.OrderCouponDto) : Async<unit> =
        async.Return()

    let updateCoupons (_conn: SqlConnection, _tran: SqlTransaction) (_dtos: Dtos.OrderCouponDto seq) : Async<unit> =
        async.Return()

    let deleteCoupon (_conn: SqlConnection, _tran: SqlTransaction) (_couponId: int) : Async<unit> = async.Return()

    let deleteCoupons (_conn: SqlConnection, _tran: SqlTransaction) (_couponIds: int seq) : Async<unit> = async.Return()

    let insertPriceData (_conn: SqlConnection, _tran: SqlTransaction) (_dto: Dtos.OrderPriceDataDto) : Async<unit> =
        async.Return()

    let insertPriceDatas
        (_conn: SqlConnection, _tran: SqlTransaction)
        (_dtos: Dtos.OrderPriceDataDto seq)
        : Async<unit> =
        async.Return()

    let updatePriceData (_conn: SqlConnection, _tran: SqlTransaction) (_dto: Dtos.OrderPriceDataDto) : Async<unit> =
        async.Return()

    let updatePriceDatas
        (_conn: SqlConnection, _tran: SqlTransaction)
        (_dtos: Dtos.OrderPriceDataDto seq)
        : Async<unit> =
        async.Return()



module Db =


    open Fling.Fling
    open Dtos
    open DbScripts
    open Domain


    module Order =


        let private loadParallel =
            createLoader orderFromDbDto (fun x -> x.OrderId)
            |> loadChild getOrderLinesForOrder
            |> loadChild getAssociatedUsersForOrder
            |> loadChild getCouponForOrder
            |> loadChild getPriceDataForOrder
            |> loadParallel


        let private loadBatchParallel =
            createBatchLoader orderFromDbDto (fun x -> x.OrderId)
            |> batchLoadChildren getOrderLinesForOrders (fun x -> x.OrderId)
            |> batchLoadChildren getAssociatedUsersForOrders (fun x -> x.OrderId)
            |> batchLoadOptChild getCouponForOrders (fun x -> x.OrderId)
            |> batchLoadChild getPriceDataForOrders (fun x -> x.OrderId)
            |> loadBatchParallel


        let private loadSerial =
            createLoader orderFromDbDto (fun x -> x.OrderId)
            |> loadChild getOrderLinesForOrder
            |> loadChild getAssociatedUsersForOrder
            |> loadChild getCouponForOrder
            |> loadChild getPriceDataForOrder
            |> loadSerial


        let private loadBatchSerial =
            createBatchLoader orderFromDbDto (fun x -> x.OrderId)
            |> batchLoadChildren getOrderLinesForOrders (fun x -> x.OrderId)
            |> batchLoadChildren getAssociatedUsersForOrders (fun x -> x.OrderId)
            |> batchLoadOptChild getCouponForOrders (fun x -> x.OrderId)
            |> batchLoadChild getPriceDataForOrders (fun x -> x.OrderId)
            |> loadBatchSerial


        let private save =
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


        let private saveBatched =
            saveRoot orderToDbDto insertOrder updateOrder
            |> batchSaveChildren
                orderToLineDtos
                (fun dto -> dto.OrderLineId)
                insertOrderLines
                updateOrderLines
                deleteOrderLines
            |> batchSaveChildren
                orderToAssociatedUserDtos
                (fun dto -> dto.OrderId, dto.UserId)
                insertOrderAssociatedUsers
                updateOrderAssociatedUsers
                deleteOrderAssociatedUsers
            |> saveOptChild orderToCouponDto (fun dto -> dto.OrderId) insertCoupon updateCoupon deleteCoupon
            |> saveChild orderToPriceDataDto insertPriceData updatePriceData


        let private saveBatchedRoot =
            Batch.saveRoot orderToDbDto insertOrders updateOrders
            |> Batch.saveChildren
                orderToLineDtos
                (fun dto -> dto.OrderLineId)
                insertOrderLines
                updateOrderLines
                deleteOrderLines
            |> Batch.saveChildren
                orderToAssociatedUserDtos
                (fun dto -> dto.OrderId, dto.UserId)
                insertOrderAssociatedUsers
                updateOrderAssociatedUsers
                deleteOrderAssociatedUsers
            |> Batch.saveOptChild orderToCouponDto (fun dto -> dto.OrderId) insertCoupons updateCoupons deleteCoupons
            |> Batch.saveChild orderToPriceDataDto insertPriceDatas updatePriceDatas


        let saveChanges connStr (oldOrder: Order option) (newOrder: Order) =
            async {
                let conn = SqlConnection connStr
                use tran = createTransaction conn
                do! save (conn, tran) oldOrder newOrder
                commit tran
            }


        let saveChangesBatched connStr (oldOrder: Order option) (newOrder: Order) =
            async {
                let conn = SqlConnection connStr
                use tran = createTransaction conn
                do! saveBatched (conn, tran) oldOrder newOrder
                commit tran
            }


        let Root connStr (orders: (Order option * Order) seq) =
            async {
                let conn = SqlConnection connStr
                use tran = createTransaction conn
                do! saveBatchedRoot (conn, tran) orders
                commit tran
            }


        let byIdParallel (conn, tran) (OrderId oid) =
            getOrderById oid |> loadParallel (conn, tran)


        let byIdSerial (conn, tran) (OrderId oid) =
            getOrderById oid |> loadSerial (conn, tran)


        let allParallel (conn, tran) : Async<Order list> =
            getAllOrders |> loadBatchParallel (conn, tran)


        let allSerial (conn, tran) : Async<Order list> =
            getAllOrders |> loadBatchSerial (conn, tran)
