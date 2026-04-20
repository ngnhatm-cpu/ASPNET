// reader.js - Isolated scope
const readerState = {
    chapterId: null,
    chapterPrice: 0,
    mangaId: null,
    prevId: null,
    nextId: null,
    lastScrollY: window.scrollY
};

document.addEventListener("DOMContentLoaded", () => {
    console.log("Reader: Initializing...");
    const toolbar = document.getElementById("readerToolbar");
    if (toolbar) {
        window.addEventListener("scroll", () => {
            if (window.scrollY > readerState.lastScrollY && window.scrollY > 100) {
                toolbar.classList.add("toolbar-hidden-top");
            } else {
                toolbar.classList.remove("toolbar-hidden-top");
            }
            readerState.lastScrollY = window.scrollY;
        });
    }

    const urlParams = new URLSearchParams(window.location.search);
    const id = urlParams.get('id');

    if (id) {
        readerState.chapterId = id;
        loadPages(id);
        
        // Wait a bit for other scripts to load
        setTimeout(() => {
            if (typeof initComments === "function") {
                initComments(id);
            }
        }, 500); 
    } else {
        showError("Không tìm thấy thông tin chương.");
    }
});

function goBack() {
    window.location.href = `detail.html?id=${readerState.mangaId || ''}`;
}

async function loadPages(cid) {
    const loader = document.getElementById("loader");
    const titleEl = document.getElementById("chapterTitle");

    try {
        const response = await fetch(`/api/content/read/${cid}`, {
            headers: {
                "Authorization": `Bearer ${localStorage.getItem('token')}`
            }
        });

        if (response.status === 401) {
            safeHideLoader();
            alert("Vui lòng đăng nhập để đọc chương này!");
            window.location.href = "login.html";
            return;
        }

        const data = await response.json().catch(() => ({}));
        readerState.mangaId = data.mangaId;

        if (response.status === 403) {
            readerState.chapterPrice = data.price || 0;
            readerState.prevId = data.prevChapterId;
            readerState.nextId = data.nextChapterId;
            updateNavButtons();
            showReaderPaywall(data);
            return;
        }

        if (!response.ok) throw new Error(data.message || "Lỗi tải nội dung");

        if (titleEl) titleEl.textContent = data.chapterTitle || "Đang đọc";
        readerState.prevId = data.prevChapterId;
        readerState.nextId = data.nextChapterId;
        
        renderPages(data.pages);
        updateNavButtons();

    } catch (error) {
        console.error("Reader Error:", error);
        showError(error.message);
    } finally {
        // Safe hide fallback
        setTimeout(safeHideLoader, 1500);
    }
}

function safeHideLoader() {
    const loader = document.getElementById("loader");
    if (loader) loader.classList.add("hidden");
}

function updateNavButtons() {
    const btnPrev = document.getElementById("btnPrev");
    const btnNext = document.getElementById("btnNext");
    
    if (btnPrev) {
        if (readerState.prevId) {
            btnPrev.disabled = false;
            btnPrev.style.opacity = "1";
        } else {
            btnPrev.disabled = true;
            btnPrev.style.opacity = "0.3";
        }
    }

    if (btnNext) {
        btnNext.disabled = !readerState.nextId;
        btnNext.style.opacity = readerState.nextId ? "1" : "0.3";
    }
}

function navigateToChapter(id) {
    if (!id) return;
    window.location.href = `reader.html?id=${id}`;
}

function showReaderPaywall(data) {
    safeHideLoader();
    const pModal = document.getElementById("paywallModal");
    if (!pModal) return;

    const userJson = localStorage.getItem("user");
    let balance = 0;
    if (userJson && userJson !== "undefined" && userJson !== "null") {
        try {
            const user = JSON.parse(userJson);
            balance = user.balance || 0;
        } catch(e) {}
    }

    const titleEl = document.getElementById("chapterTitle");
    if (titleEl) titleEl.textContent = data.chapterTitle || "Chương Khóa";
    
    const mPrice = document.getElementById("modalPrice");
    const mBalance = document.getElementById("modalBalance");
    if (mPrice) mPrice.textContent = `${data.price} Xu`;
    if (mBalance) mBalance.textContent = `${balance} Xu`;
    
    pModal.classList.remove("hidden");
    pModal.classList.add("flex");
    setTimeout(() => { pModal.classList.remove("opacity-0"); }, 50);
}

async function buyChapterInReader() {
    const btn = document.getElementById("btnConfirmBuy");
    if (!btn) return;

    try {
        btn.textContent = "Đang xử lý...";
        btn.disabled = true;

        await apiFetch("/Orders", {
            method: "POST",
            body: JSON.stringify({ chapterIds: [parseInt(readerState.chapterId)] })
        });

        // Update local balance
        const userJson = localStorage.getItem("user");
        if (userJson) {
            const user = JSON.parse(userJson);
            user.balance -= readerState.chapterPrice;
            localStorage.setItem("user", JSON.stringify(user));
        }

        window.location.reload();
    } catch (err) {
        const errP = document.getElementById("modalError");
        if (errP) {
            errP.textContent = err.message || "Lỗi giao dịch.";
            errP.classList.remove("hidden");
        }
        btn.disabled = false;
        btn.textContent = "Thử lại";
    }
}

function renderPages(pages) {
    safeHideLoader();
    const container = document.getElementById("pagesContainer");
    if (!container) return;

    if (!pages || pages.length === 0) {
        showError("Chương này đang được tác giả cập nhật hình ảnh.");
        return;
    }

    container.innerHTML = "";
    pages.forEach((url, i) => {
        const img = document.createElement("img");
        img.className = "page-img";
        img.src = url;
        img.alt = `Trang ${i + 1}`;
        img.loading = "lazy";
        container.appendChild(img);
    });
}

function showError(msg) {
    safeHideLoader();
    const errContainer = document.getElementById("errorContainer");
    const errMsg = document.getElementById("errorMsg");
    if (errContainer) errContainer.classList.remove("hidden");
    if (errMsg) errMsg.textContent = msg;

    const bNav = document.getElementById("bottomNav");
    if (bNav) bNav.style.opacity = "0.5";
}
