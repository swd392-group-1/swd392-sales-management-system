(() => {
 const status = document.getElementById("price-update");
 const connection = new signalR.HubConnectionBuilder().withUrl("/hubs/prices").withAutomaticReconnect().build();
 connection.on("BookPriceChanged", p => {
   status.textContent = `Giá danh mục sách ${p.bookId} vừa đổi thành ${p.currentPrice}. Tải lại danh mục để lấy giá mới; kiểm tra lại đơn trước khi xác nhận.`;
 });
 connection.onreconnected(() => status.textContent = "Đã kết nối lại. Tải lại danh mục để lấy giá hiện tại.");
 connection.start().catch(() => status.textContent = "Chưa nhận được cập nhật giá trực tiếp. Tải lại danh mục để lấy giá hiện tại.");
})();
