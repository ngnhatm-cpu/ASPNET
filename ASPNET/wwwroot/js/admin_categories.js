document.addEventListener("DOMContentLoaded", async () => {
    checkAdmin();
    await loadCategories();
});

const categoryModal = document.getElementById("categoryModal");
const categoryForm = document.getElementById("categoryForm");

async function loadCategories() {
    const tableBody = document.getElementById("categoryTableBody");
    try {
        const categories = await apiFetch("/Categories");
        tableBody.innerHTML = categories.map(c => {
            const defaultImg = "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSI0OCIgaGVpZ2h0PSI0OCI+PHJlY3Qgd2lkdGg9IjEwMCUiIGhlaWdodD0iMTAwJSIgZmlsbD0iIzFmMWYyNSIvPjx0ZXh0IHg9IjUwJSIgeT0iNTAlIiBmb250LWZhbWlseT0ic2Fucy1zZXJpZiIgZm9udC1zaXplPSI4IiBmaWxsPSIjNTU1IiBkeT0iLjNlbSIgdGV4dC1hbmNob3I9Im1pZGRsZSI+Q0FUPC90ZXh0Pjwvc3ZnPg==";
            const imgSrc = c.imageUrl ? c.imageUrl : defaultImg;
            return `
            <tr class="border-b border-white/5 hover:bg-white/5 transition-all group">
                <td class="px-6 py-4 font-mono text-white/40 text-xs">#CAT-${c.id}</td>
                <td class="px-6 py-4">
                    <img src="${imgSrc}" onerror="this.src='${defaultImg}'" class="w-12 h-12 object-cover rounded shadow-lg">
                </td>
                <td class="px-6 py-4 font-bold text-white">${c.name}</td>
                <td class="px-6 py-4 text-right space-x-2">
                    <button onclick="editCategory(${c.id}, '${c.name}', '${c.imageUrl || ''}')" class="text-[#6fd8cc] hover:text-white transition-colors"><span class="material-symbols-outlined">edit</span></button>
                    <button onclick="deleteCategory(${c.id})" class="text-[#ff535b] hover:text-white transition-colors"><span class="material-symbols-outlined">delete</span></button>
                </td>
            </tr>
        `}).join('');
    } catch (e) { console.error(e); }
}

function openCategoryModal(categoryId = "", categoryName = "", imageUrl = "") {
    categoryModal.classList.remove("hidden");
    document.getElementById("categoryId").value = categoryId;
    document.getElementById("categoryName").value = categoryName;
    document.getElementById("categoryImageUrl").value = imageUrl;
    document.getElementById("categoryImageFile").value = "";
    document.getElementById("modalTitle").textContent = categoryId ? "Cập Nhật Thể Loại" : "Thêm Thể Loại";

    const preview = document.getElementById("categoryPreview");
    if (imageUrl) {
        preview.src = imageUrl;
        preview.classList.remove("hidden");
    } else {
        preview.classList.add("hidden");
    }
}

function closeCategoryModal() {
    categoryModal.classList.add("hidden");
    categoryForm.reset();
}

document.getElementById("categoryImageFile").addEventListener("change", async (e) => {
    const file = e.target.files[0];
    if (!file) return;

    const formData = new FormData();
    formData.append("file", file);

    try {
        const response = await fetch("/api/Upload/image", {
            method: "POST",
            headers: { "Authorization": `Bearer ${localStorage.getItem("token")}` },
            body: formData
        });

        if (!response.ok) throw new Error("Lỗi tải ảnh");
        const data = await response.json();
        
        document.getElementById("categoryImageUrl").value = data.url;
        document.getElementById("categoryPreview").src = data.url;
        document.getElementById("categoryPreview").classList.remove("hidden");
    } catch (err) { alert(err.message); }
});

categoryForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    const id = document.getElementById("categoryId").value;
    const body = {
        name: document.getElementById("categoryName").value,
        imageUrl: document.getElementById("categoryImageUrl").value
    };

    try {
        if (id) {
            body.id = parseInt(id);
            await apiFetch(`/Categories/${id}`, { method: "PUT", body: JSON.stringify(body) });
        } else {
            await apiFetch("/Categories", { method: "POST", body: JSON.stringify(body) });
        }
        closeCategoryModal();
        await loadCategories();
    } catch (error) {
        alert("Lỗi: " + error.message);
    }
});

function editCategory(id, name, imageUrl) {
    openCategoryModal(id, name, imageUrl);
}

async function deleteCategory(id) {
    if (!confirm("Bạn có chắc chắn muốn xóa thể loại này?")) return;
    try {
        await apiFetch(`/Categories/${id}`, { method: "DELETE" });
        await loadCategories();
    } catch (e) { alert(e.message); }
}
