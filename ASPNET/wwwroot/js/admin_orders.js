document.addEventListener("DOMContentLoaded", async () => {
    checkAdmin();
    await loadOrders();
});

const orderModal = document.getElementById("orderModal");
let allOrders = [];

async function loadOrders() {
    const tableBody = document.getElementById("ordersTableBody");
    try {
        allOrders = await apiFetch("/Orders");
        tableBody.innerHTML = allOrders.map(o => `
            <tr class="border-b border-white/5 hover:bg-white/5 transition-all group">
                <td class="px-6 py-4 font-mono text-white/40 text-xs">#MS-${o.id}</td>
                <td class="px-6 py-4 font-bold text-white">${o.customerName}</td>
                <td class="px-6 py-4 text-white/60">${new Date(o.orderDate).toLocaleString('vi-VN')}</td>
                <td class="px-6 py-4 font-black">${new Intl.NumberFormat('vi-VN').format(o.totalAmount)} Xu</td>
                <td class="px-6 py-4">
                    <span class="px-2 py-1 rounded text-[10px] font-black uppercase tracking-widest ${o.status === 'Completed' ? 'bg-[#6fd8cc]/20 text-[#6fd8cc]' : 'bg-white/5 text-white/40'}">
                        ${o.status}
                    </span>
                </td>
                <td class="px-6 py-4 text-right">
                    <button onclick="viewOrder(${o.id})" class="text-[#6fd8cc] hover:text-white transition-colors"><span class="material-symbols-outlined">visibility</span></button>
                </td>
            </tr>
        `).join('');
    } catch (e) {
        console.error(e);
        tableBody.innerHTML = `<tr><td colspan="6" class="text-center py-4 text-red-500">Lỗi khi tải đơn hàng</td></tr>`;
    }
}

function viewOrder(id) {
    const order = allOrders.find(o => o.id === id);
    if (!order) return;

    document.getElementById("orderIdLabel").textContent = order.id;
    document.getElementById("orderCustomer").textContent = order.customerName;
    document.getElementById("orderDate").textContent = new Date(order.orderDate).toLocaleString('vi-VN');
    document.getElementById("orderTotal").textContent = `${new Intl.NumberFormat('vi-VN').format(order.totalAmount)} Xu`;

    const itemsBody = document.getElementById("orderItemsBody");
    itemsBody.innerHTML = order.items.map(item => `
        <tr class="border-b border-white/5 last:border-0 hover:bg-white/5">
            <td class="px-4 py-3 text-white/60">${item.mangaTitle || 'N/A'}</td>
            <td class="px-4 py-3 font-bold text-white">${item.chapterTitle}</td>
            <td class="px-4 py-3 text-right font-mono">${new Intl.NumberFormat('vi-VN').format(item.unitPrice)}</td>
        </tr>
    `).join('');

    orderModal.classList.remove("hidden");
}

function closeOrderModal() {
    orderModal.classList.add("hidden");
}
