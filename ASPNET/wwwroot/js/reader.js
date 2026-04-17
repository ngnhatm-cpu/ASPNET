let currentChapterId = null;
let currentChapterPrice = 0;
let currentMangaId = null;
let globalPrevId = null;
let globalNextId = null;

let lastScrollY = window.scrollY;
const toolbar = document.getElementById("readerToolbar");

window.addEventListener("scroll", () => {
    if (window.scrollY > lastScrollY && window.scrollY > 100) {
        toolbar.classList.add("toolbar-hidden-top");
    } else {
        toolbar.classList.remove("toolbar-hidden-top");
    }
    lastScrollY = window.scrollY;
});

document.addEventListener("DOMContentLoaded", () => {
    const urlParams = new URLSearchParams(window.location.search);
    const chapterId = urlParams.get('id');

    if (chapterId) {
        currentChapterId = chapterId;
        loadPages(chapterId);
    } else {
        showError("Không tìm thấy thông tin chương.");
    }
});

function goBack() {
    window.location.href = `detail.html?id=${currentMangaId || ''}`; // If we have mangaId, or just home
}

async function loadPages(chapterId) {
    try {
        const response = await fetch(`/api/content/read/${chapterId}`, {
            headers: {
                "Authorization": `Bearer ${localStorage.getItem('token')}`
            }
        });

        const data = await response.json();
        currentMangaId = data.mangaId;

        if (response.status === 403) {
            // Unpurchased -> Show Paywall
            currentChapterPrice = data.price;
            globalPrevId = data.prevChapterId;
            globalNextId = data.nextChapterId;
            updateNavButtons();
            showReaderPaywall(data);
            return;
        }

        if (response.status === 401) {
            alert("Vui lòng đăng nhập để đọc chương này!");
            window.location.href = "login.html";
            return;
        }

        if (!response.ok) throw new Error(data.message || "Lấy dữ liệu truyện thất bại.");

        document.getElementById("chapterTitle").textContent = data.chapterTitle || "Đang đọc";
        globalPrevId = data.prevChapterId;
        globalNextId = data.nextChapterId;
        renderPages(data.pages);
        updateNavButtons();

    } catch (error) {
        showError(error.message);
    }
}

function updateNavButtons() {
    const btnPrev = document.getElementById("btnPrev");
    const btnNext = document.getElementById("btnNext");
    
    if (globalPrevId) {
        btnPrev.disabled = false;
        btnPrev.classList.replace("text-white/30", "text-white/50");
    } else {
        btnPrev.disabled = true;
        btnPrev.classList.replace("text-white/50", "text-white/30");
    }

    if (globalNextId) {
        btnNext.disabled = false;
    } else {
        btnNext.disabled = true;
    }
}

function navigateToChapter(id) {
    if (!id) return;
    window.location.href = `reader.html?id=${id}`;
}

function showReaderPaywall(data) {
    const pModal = document.getElementById("paywallModal");
    const pContent = document.getElementById("paywallContent");
    
    const user = JSON.parse(localStorage.getItem("user") || "{}");
    const balance = user.balance || 0;

    document.getElementById("chapterTitle").textContent = data.chapterTitle || "Chương Khóa";
    document.getElementById("modalPrice").textContent = `${data.price} Xu`;
    document.getElementById("modalBalance").textContent = `${balance} Xu`;
    
    document.getElementById("loader").classList.add("hidden");
    pModal.classList.remove("hidden");
    pModal.classList.add("flex");
    setTimeout(() => {
        pModal.classList.remove("opacity-0");
        pContent.classList.remove("scale-95");
    }, 10);
}

async function buyChapterInReader() {
    try {
        const btn = document.getElementById("btnConfirmBuy");
        const originalText = btn.textContent;
        btn.textContent = "Đang xử lý...";
        btn.disabled = true;

        const res = await apiFetch("/Orders", {
            method: "POST",
            body: JSON.stringify({ chapterIds: [parseInt(currentChapterId)] })
        });

        // Update local balance
        const user = JSON.parse(localStorage.getItem("user"));
        user.balance -= currentChapterPrice;
        localStorage.setItem("user", JSON.stringify(user));

        // Close paywall and reload
        const pModal = document.getElementById("paywallModal");
        pModal.classList.add("hidden");
        window.location.reload();

    } catch (err) {
        const errP = document.getElementById("modalError");
        errP.textContent = err.message || "Lỗi giao dịch.";
        errP.classList.remove("hidden");
        document.getElementById("btnConfirmBuy").disabled = false;
        document.getElementById("btnConfirmBuy").textContent = "Thử lại";
    }
}

function renderPages(pages) {
    const loader = document.getElementById("loader");
    const container = document.getElementById("pagesContainer");
    loader.classList.add("hidden");

    if (!pages || pages.length === 0) {
        showError("Chương này đang được tác giả cập nhật hình ảnh.");
        return;
    }

    pages.forEach((url, i) => {
        const img = document.createElement("img");
        img.className = "page-img";
        img.src = url;
        img.alt = `Trang ${i + 1}`;
        img.loading = "lazy"; // Tối ưu load
        
        container.appendChild(img);
    });
}

function showError(msg) {
    document.getElementById("loader").classList.add("hidden");
    const errContainer = document.getElementById("errorContainer");
    errContainer.classList.remove("hidden");
    document.getElementById("errorMsg").textContent = msg;
    document.getElementById("bottomNav").classList.add("hidden");
}
