namespace BookSalesManagement.Application.Common;

public static class PermissionCodes
{
    public const string MaintainProviderInformation = "DEMO-20261027-UC04";
    public const string PreparePurchaseOrder = "DEMO-20261027-UC05";
    public const string RecordGoodsReceipt = "DEMO-20261027-UC06";
    public const string RecordInventoryCount = "DEMO-20261027-UC07";
    public const string RecordInventoryAdjustment = "DEMO-20261027-UC08";
    public const string PerformInventoryAudit = "DEMO-20261027-UC09";
    public const string ApprovePurchaseOrder = "DEMO-20261027-UC13";
    public const string ApproveInventoryAudit = "DEMO-20261027-UC14";

    public static string Describe(string permissionId) => permissionId switch
    {
        MaintainProviderInformation => "UC04 - Duy trì thông tin nhà cung cấp",
        PreparePurchaseOrder => "UC05 - Lập đơn mua hàng",
        RecordGoodsReceipt => "UC06 - Ghi nhận nhập hàng",
        RecordInventoryCount => "UC07 - Kiểm kê tồn kho",
        RecordInventoryAdjustment => "UC08 - Ghi nhận điều chỉnh tồn",
        PerformInventoryAudit => "UC09 - Thực hiện audit tồn kho",
        ApprovePurchaseOrder => "UC13 - Duyệt đơn mua hàng",
        ApproveInventoryAudit => "UC14 - Duyệt audit tồn kho",
        _ => permissionId
    };
}
