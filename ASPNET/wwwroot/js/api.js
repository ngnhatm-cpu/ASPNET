const API_BASE = "/api";

async function apiFetch(endpoint, options = {}) {
    const token = localStorage.getItem("token");
    const headers = {
        "Content-Type": "application/json",
        ...options.headers,
    };

    if (token) {
        headers["Authorization"] = `Bearer ${token}`;
    }

    const response = await fetch(`${API_BASE}${endpoint}`, {
        ...options,
        headers,
    });

    if (response.status === 401) {
        // Token hết hạn hoặc không hợp lệ
        localStorage.removeItem("token");
        localStorage.removeItem("user");
        const path = window.location.pathname;
        if (path.includes("/admin/")) {
            window.location.href = "/admin/login.html";
        } else {
            window.location.href = "/login.html";
        }
        return;
    }

    if (!response.ok) {
        const error = await response.json().catch(() => ({ message: "Đã có lỗi xảy ra" }));
        throw new Error(error.message || "Lỗi API");
    }

    if (response.status === 204) return null;
    return await response.json();
}

function checkAdmin() {
    const user = JSON.parse(localStorage.getItem("user") || "{}");
    if (user.role !== "Admin") {
        window.location.href = "/admin/login.html";
    }
}
