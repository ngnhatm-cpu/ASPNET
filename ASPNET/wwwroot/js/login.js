const loginForm = document.getElementById("loginForm");
const errorMsg = document.getElementById("errorMsg");

loginForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    errorMsg.classList.add("hidden");

    const username = document.getElementById("username").value;
    const password = document.getElementById("password").value;

    try {
        const response = await fetch("/api/Auth/login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username, password }),
        });

        if (!response.ok) {
            const data = await response.json();
            throw new Error(data.message || "Tên đăng nhập hoặc mật khẩu sai");
        }

        const data = await response.json();

        // Kiểm tra xem có phải Admin không
        if (data.user.role !== "Admin") {
            throw new Error("Tài khoản này không có quyền truy cập Admin!");
        }

        // Lưu Token và thông tin
        localStorage.setItem("token", data.token);
        localStorage.setItem("user", JSON.stringify(data.user));

        // Chuyển hướng
        window.location.href = "/admin/index.html";
    } catch (error) {
        errorMsg.textContent = error.message;
        errorMsg.classList.remove("hidden");
    }
});
