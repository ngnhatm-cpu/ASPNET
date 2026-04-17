document.addEventListener("DOMContentLoaded", async () => {
    checkAdmin();
    await loadStats();
    
    document.getElementById("logoutBtn").addEventListener("click", () => {
        localStorage.removeItem("token");
        localStorage.removeItem("user");
        window.location.href = "/admin/login.html";
    });
});

async function loadStats() {
    try {
        const stats = await apiFetch("/Dashboard/stats");
        
        // Cập nhật các con số
        document.getElementById("statRevenue").textContent = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(stats.totalRevenue);
        document.getElementById("statOrders").textContent = stats.newOrdersCount;
        document.getElementById("statUsers").textContent = stats.totalUsersCount;
        document.getElementById("statChapters").textContent = stats.totalChaptersCount;

        // Render bảng đơn hàng gần đây
        const tableBody = document.getElementById("recentOrdersTable");
        if (stats.recentOrders.length === 0) {
            tableBody.innerHTML = `<tr><td colspan="5" class="text-center py-10 text-white/20 uppercase tracking-widest text-[10px] font-bold">Chưa có giao dịch nào</td></tr>`;
            return;
        }

        tableBody.innerHTML = stats.recentOrders.map(order => `
            <tr class="bg-[#2a292f] group hover:bg-[#35343a] transition-all">
                <td class="py-4 px-4 rounded-l-lg font-mono text-white/40 text-xs">#MS-${order.id}</td>
                <td class="py-4 px-4 text-white/60 text-xs">${new Date(order.orderDate).toLocaleString('vi-VN')}</td>
                <td class="py-4 px-4 font-bold text-sm text-white">${order.customerName}</td>
                <td class="py-4 px-4 font-black text-[#ff535b]">${new Intl.NumberFormat('vi-VN').format(order.totalAmount)}đ</td>
                <td class="py-4 px-4 rounded-r-lg">
                    <span class="bg-[#6fd8cc]/10 text-[#6fd8cc] px-3 py-1 rounded-full text-[10px] font-bold uppercase tracking-tighter border border-[#6fd8cc]/20">
                        ${order.status}
                    </span>
                </td>
            </tr>
        `).join('');

    } catch (error) {
        console.error("Lỗi khi tải Dashboard:", error);
    }
}
