(() => {
    const form = document.querySelector("[data-sale-form]");
    const totalOutput = document.getElementById("sale-total");
    const validationMessage = document.getElementById("sale-validation-message");
    const searchInput = document.getElementById("book-search");
    const bookListEmpty = document.getElementById("book-list-empty");
    const tableWrap = document.querySelector(".sale-table-wrap");

    if (!form || !totalOutput || !validationMessage) {
        return;
    }

    const integerFormat = new Intl.NumberFormat("vi-VN", { maximumFractionDigits: 0 });
    const stockScale = 10000000000n;

    const filterBooks = () => {
        if (!searchInput || !tableWrap) {
            return;
        }

        const searchTerm = searchInput.value.trim().toLowerCase();
        const rows = tableWrap.querySelectorAll(".sale-row");
        let visibleCount = 0;

        for (const row of rows) {
            const bookId = row.querySelector(".sale-book-id")?.textContent.trim().toLowerCase() ?? "";
            const title = row.querySelector(".sale-title")?.textContent.trim().toLowerCase() ?? "";
            const matches = !searchTerm || bookId.includes(searchTerm) || title.includes(searchTerm);
            row.hidden = !matches;
            if (matches) {
                visibleCount++;
            }
        }

        if (bookListEmpty) {
            bookListEmpty.hidden = visibleCount > 0;
        }
    };

    const parseScaled = (value, fractionDigits) => {
        const [whole, fraction = ""] = value.split(".");
        if (!/^\d+$/.test(whole) || (fraction && !/^\d+$/.test(fraction)) || fraction.length > fractionDigits) {
            return null;
        }
        return BigInt(whole) * (10n ** BigInt(fractionDigits)) + BigInt(fraction.padEnd(fractionDigits, "0"));
    };

    const formatCurrency = scaledAmount => {
        const whole = integerFormat.format(scaledAmount / 10000n);
        const fraction = (scaledAmount % 10000n).toString().padStart(4, "0").replace(/0+$/, "");
        return whole + (fraction ? "," + fraction : "") + " \u20AB";
    };

    const updateTotal = () => {
        let total = 0n;

        for (const row of form.querySelectorAll(".sale-row")) {
            const quantityInput = row.querySelector(".sale-quantity");
            const lineTotal = row.querySelector(".sale-line-total");
            const error = row.querySelector(".sale-row-error");
            const quantityText = quantityInput?.value.trim() ?? "";
            const price = parseScaled(row.dataset.price ?? "", 4);
            const stock = parseScaled(row.dataset.stock ?? "", 10);
            const validQuantity = /^\d+$/.test(quantityText);
            const quantity = validQuantity ? BigInt(quantityText) : 0n;
            const exceedsStock = stock !== null && quantity * stockScale > stock;
            const invalid = !quantityInput?.disabled && (!validQuantity || exceedsStock);

            quantityInput?.setAttribute("aria-invalid", invalid ? "true" : "false");
            row.classList.toggle("has-sale-error", invalid);
            if (error) {
                error.textContent = exceedsStock
                    ? "Số lượng mua vượt quá tồn kho hiện tại."
                    : validQuantity ? "" : "Số lượng phải là số nguyên không âm.";
                error.hidden = !invalid;
            }

            if (invalid || price === null || price <= 0n || quantity <= 0n) {
                if (lineTotal) {
                    lineTotal.textContent = invalid ? "—" : formatCurrency(0n);
                }
                continue;
            }

            const amount = price * quantity;
            total += amount;
            if (lineTotal) {
                lineTotal.textContent = formatCurrency(amount);
            }
        }

        totalOutput.textContent = formatCurrency(total);
    };

    form.addEventListener("change", updateTotal);
    form.addEventListener("input", updateTotal);

    if (searchInput) {
        searchInput.addEventListener("input", filterBooks);
    }

    window.addEventListener("pageshow", updateTotal);

    form.addEventListener("submit", event => {
        updateTotal();
        const hasInvalidQuantity = [...form.querySelectorAll(".sale-quantity")]
            .some(input => !input.disabled && input.getAttribute("aria-invalid") === "true");
        const hasPurchasedBook = [...form.querySelectorAll(".sale-quantity")]
            .some(input => !input.disabled && /^\d+$/.test(input.value.trim()) && BigInt(input.value.trim()) > 0n && input.getAttribute("aria-invalid") !== "true");

        if (hasInvalidQuantity || !hasPurchasedBook) {
            event.preventDefault();
            validationMessage.textContent = hasInvalidQuantity
                ? "Vui lòng sửa số lượng không hợp lệ trước khi hoàn tất bán hàng."
                : "Vui lòng nhập số lượng lớn hơn 0 cho ít nhất một sách.";
            validationMessage.hidden = false;
            form.querySelector('.sale-quantity[aria-invalid="true"]')?.focus();
            return;
        }

        validationMessage.hidden = true;
    });
    updateTotal();
})();
 