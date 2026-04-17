document.addEventListener("DOMContentLoaded", () => {
    const loginForm = document.getElementById("loginForm");
    const registerForm = document.getElementById("registerForm");
    const errorMsg = document.getElementById("errorMessage");

    function showError(message) {
        if (errorMsg) {
            errorMsg.textContent = message;
            errorMsg.classList.remove("hidden");
        } else {
            alert(message);
        }
    }

    if (loginForm) {
        loginForm.addEventListener("submit", async (e) => {
            e.preventDefault();
            const usernameInput = document.getElementById("username").value;
            const passwordInput = document.getElementById("password").value;

            try {
                // apiFetch is imported from api.js.
                // However, since we don't have token yet, we just use standard fetch 
                // or apiFetch handles it gracefully.
                const res = await fetch("/api/Auth/login", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ username: usernameInput, password: passwordInput })
                });

                const data = await res.json();
                if (!res.ok) {
                    throw new Error(data.message || "Tên đăng nhập hoặc mật khẩu không đúng.");
                }

                // Luu thong tin dang nhap
                localStorage.setItem("token", data.token);
                
                const user = data.user; // Dữ liệu user nằm trong thuộc tính user của response
                localStorage.setItem("user", JSON.stringify({
                    id: user.id,
                    username: user.username,
                    email: user.email,
                    role: user.role,
                    balance: user.balance || 0
                }));

                // Chuyển hướng
                if (user.role === "Admin") {
                    window.location.href = "/admin/index.html";
                } else {
                    window.location.href = "/index.html";
                }

            } catch (err) {
                showError(err.message);
            }
        });
    }

    if (registerForm) {
        registerForm.addEventListener("submit", async (e) => {
            e.preventDefault();
            const usernameInput = document.getElementById("username").value;
            const emailInput = document.getElementById("email").value;
            const passwordInput = document.getElementById("password").value;

            try {
                const res = await fetch("/api/Auth/register", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ username: usernameInput, email: emailInput, password: passwordInput })
                });

                const data = await res.json();
                if (!res.ok) {
                    throw new Error(data.message || "Đăng ký thất bại.");
                }

                alert("Đăng ký thành công! Vui lòng đăng nhập.");
                window.location.href = "login.html";
            } catch (err) {
                showError(err.message);
            }
        });
    }
});
