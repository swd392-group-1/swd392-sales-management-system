(() => {
    "use strict";
    const form = document.getElementById("notice-form");
    const status = document.getElementById("connection-status");
    const sendStatus = document.getElementById("send-status");
    const submit = document.getElementById("submit-notice");
    const reconnect = document.getElementById("connect");
    const list = document.getElementById("received-notices");
    const connection = new signalR.HubConnectionBuilder().withUrl("/hubs/development").withAutomaticReconnect().build();
    const connected = () => connection.state === signalR.HubConnectionState.Connected;
    async function start() {
        reconnect.disabled = true;
        try {
            await connection.start();
            status.textContent = "Đã kết nối";
            submit.disabled = false;
        } catch {
            status.textContent = "Chưa kết nối. Hãy thử kết nối lại.";
            reconnect.disabled = false;
            submit.disabled = true;
        }
    }
    connection.on("NoticeReceived", notice => {
        document.getElementById("empty-state").hidden = true;
        const item = document.createElement("li");
        const message = document.createElement("p");
        message.textContent = notice.message;
        const meta = document.createElement("small");
        meta.textContent = new Date(notice.createdAtUtc).toLocaleTimeString("vi-VN") + " · " + notice.id;
        item.append(message, meta);
        list.prepend(item);
        if (list.children.length > 100) list.lastElementChild.remove();
    });
    connection.onreconnecting(() => { status.textContent = "Đang kết nối lại…"; submit.disabled = true; });
    connection.onreconnected(() => { status.textContent = "Đã kết nối"; submit.disabled = false; });
    connection.onclose(() => {
        status.textContent = "Đã mất kết nối. Hãy kết nối lại.";
        submit.disabled = true;
        reconnect.disabled = false;
    });
    reconnect.addEventListener("click", start);
    form.addEventListener("submit", async event => {
        event.preventDefault();
        if (!form.reportValidity() || !connected()) return;
        submit.disabled = true;
        sendStatus.textContent = "Đang gửi…";
        try {
            const response = await fetch("/development/notices", {
                method: "POST", credentials: "same-origin",
                headers: { "Content-Type": "application/json", "RequestVerificationToken": form.querySelector('input[name="__RequestVerificationToken"]').value },
                body: JSON.stringify({ message: document.getElementById("message").value })
            });
            if (!response.ok) throw new Error("submit failed");
            const result = await response.json();
            sendStatus.textContent = "Đã xếp hàng: " + result.id + ". Theo dõi kết quả tại danh sách thông báo.";
        } catch {
            sendStatus.textContent = "Không gửi được thông báo. Kiểm tra nội dung và kết nối rồi thử lại.";
        } finally { submit.disabled = !connected(); }
    });
    start();
})();
