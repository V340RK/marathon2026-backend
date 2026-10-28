using System;

namespace APIHandler.Constants;

public static class ResourceNames
{
    public static string Region =>
        Environment.GetEnvironmentVariable("REGION")
        ?? throw new InvalidOperationException("Environment variable 'REGION' is not set.");

    public static string FeedbackTableName =>
        Environment.GetEnvironmentVariable("FEEDBACK_TABLE_NAME")
        ?? throw new ArgumentException("Environment variable 'FEEDBACK_TABLE_NAME' is not set.");

    public static string WaiterListTableName =>
        Environment.GetEnvironmentVariable("WaiterListTableName")
        ?? throw new ArgumentException("Environment variable 'WaiterListTableName' is not set.");

    public static string LocationsTableName =>
        Environment.GetEnvironmentVariable("locationsTable")
        ?? throw new ArgumentException("Environment variable 'locationsTable' is not set.");

    public static string DishesTableName =>
        Environment.GetEnvironmentVariable("dishesTable")
        ?? throw new ArgumentException("Environment variable 'dishesTable' is not set.");

    public static string ProfileImagesTableName =>
        Environment.GetEnvironmentVariable("profileImagesTable")
        ?? throw new ArgumentException("Environment variable 'profileImagesBucketName' is not set.");

    public static string ReportsTableName =>
        Environment.GetEnvironmentVariable("reportsTable")
        ?? throw new ArgumentException("Environment variable 'reportsTable' is not set.");

    public static string ReportsHistoryTableName =>
        Environment.GetEnvironmentVariable("reportsHistoryTable")
        ?? throw new ArgumentException("Environment variable 'reportsHistoryTable' is not set.");

    public static string UserPoolId =>
        Environment.GetEnvironmentVariable("COGNITO_USER_POOL_ID")
        ?? throw new ArgumentException("Environment variable COGNITO_USER_POOL_ID environment variable is not set");

    public static string ClientId =>
        Environment.GetEnvironmentVariable("COGNITO_USER_POOL_CLIENT_ID")
        ?? throw new ArgumentException("Environment variable COGNITO_USER_POOL_CLIENT_ID environment variable is not set");

    public static string ReservationsTableName =>
        Environment.GetEnvironmentVariable("reservationsTable")
        ?? throw new ArgumentException("Environment variable 'reservationsTable' is not set.");

    public static string GroupReservationsTableName =>
        Environment.GetEnvironmentVariable("groupReservationsTable")
        ?? throw new ArgumentException("Environment variable 'groupReservationsTable' is not set.");

    public static string TablesTableName =>
        Environment.GetEnvironmentVariable("tablesTable")
        ?? throw new ArgumentException("Environment variable 'tablesTable' is not set.");

    public static string OrdersTableName =>
        Environment.GetEnvironmentVariable("ordersTable")
        ?? throw new ArgumentException("Environment variable 'ordersTable' is not set.");

    public static string NotificationsTableName =>
        Environment.GetEnvironmentVariable("notificationsTable")
        ?? throw new ArgumentException("Environment variable 'notificationTable' is not set.");

    public static string NotificationsUserIdIndex =>
        Environment.GetEnvironmentVariable("notificationsUserIdIndex")
        ?? throw new ArgumentException("Environment variable 'notificationsUserIdIndex' is not set.");

    public static string OrdersTableCustomerIdGlobalIndex => "GSI_CustomerId";

    public static string OrdersTableReservationIdGlobalIndex => "GSI_ReservationId";

    public static string ReportsHistoryTableUserIdGlobalIndex => "UserIdIndex";

    public static string ReservationsTableGlobalIndex =>
        Environment.GetEnvironmentVariable("reservationsTableGlobalIndex")
        ?? throw new ArgumentException("Environment variable 'reservationsTableGlobalIndex' is not set.");

    public static string WaitersGlobalIndex =>
        Environment.GetEnvironmentVariable("waitersGlobalIndex")
        ?? throw new ArgumentException("Environment variable 'waitersGlobalIndex' is not set.");

    public static string ReservationsCustomerIdGlobalIndex =>
        Environment.GetEnvironmentVariable("reservationsCustomerIdGlobalIndex")
        ?? throw new ArgumentException("Environment variable 'reservationsCustomerIdGlobalIndex' is not set.");

    public static string ReservationsWaiterIdGlobalIndex =>
        Environment.GetEnvironmentVariable("reservationsWaiterIdGlobalIndex")
        ?? throw new ArgumentException("Environment variable 'reservationsWaiterIdGlobalIndex' is not set.");

    public static string ImagesBucketName =>
        Environment.GetEnvironmentVariable("imagesBucketName")
        ?? throw new ArgumentException("Environment variable 'imagesBucketName' is not set.");

    public static string FeedbackGlobalIndex =>
        Environment.GetEnvironmentVariable("feedbackGlobalIndex")
        ?? throw new ArgumentException("Environment variable 'feedbackGlobalIndex' is not set.");

    public static string ProfileImagesBucketName =>
        Environment.GetEnvironmentVariable("profileImagesBucketName")
        ?? throw new ArgumentException("Environment variable 'profileImagesBucketName' is not set.");

    public static string OrdersIndexReservationId =>
        Environment.GetEnvironmentVariable("OrderIndexReservationId")
        ?? throw new ArgumentException("Environment variable 'OrderIndexReservationId' is not set.");

    public static string ReportQueueName =>
        Environment.GetEnvironmentVariable("reports_queue_name")
        ?? throw new ArgumentException("Environment variable 'REPORT_QUEUE_NAME' is not set.");

    public static string GuestSessionTableName =>
        Environment.GetEnvironmentVariable("guestSessionTable")
        ?? throw new InvalidOperationException("Environment variable 'guestSessionTable' is not set.");

    public static string TablesLocationIdIndex => Environment.GetEnvironmentVariable("tableLocationIdIndex")
        ?? throw new InvalidOperationException("Environment variable 'tableLocationIdIndex' is not set.");
}