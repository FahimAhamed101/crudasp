"use client";

import { useEffect, useMemo, useState } from "react";
import {
  Product,
  useCreateProductMutation,
  useDeleteProductMutation,
  useGetProductsQuery,
  useUpdateProductMutation,
} from "./api";

const apiBaseUrl =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5000";

type FormState = {
  name: string;
  description: string;
  price: string;
  quantity: string;
};

const emptyForm: FormState = {
  name: "",
  description: "",
  price: "",
  quantity: "",
};

function getImageSrc(imageUrl?: string | null) {
  if (!imageUrl) return null;
  if (imageUrl.startsWith("http")) return imageUrl;
  return `${apiBaseUrl}${imageUrl}`;
}

export default function Home() {
  const { data: products = [], isLoading, isError } = useGetProductsQuery();
  const [createProduct, createStatus] = useCreateProductMutation();
  const [updateProduct, updateStatus] = useUpdateProductMutation();
  const [deleteProduct, deleteStatus] = useDeleteProductMutation();

  const [form, setForm] = useState<FormState>(emptyForm);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const isSaving = createStatus.isLoading || updateStatus.isLoading;

  useEffect(() => {
    if (!imageFile) return;
    const url = URL.createObjectURL(imageFile);
    setPreviewUrl(url);
    return () => URL.revokeObjectURL(url);
  }, [imageFile]);

  const stats = useMemo(() => {
    const totalQty = products.reduce((sum, p) => sum + p.quantity, 0);
    const totalValue = products.reduce((sum, p) => sum + p.price * p.quantity, 0);
    return { totalQty, totalValue };
  }, [products]);

  const startEdit = (product: Product) => {
    setEditingId(product.id);
    setForm({
      name: product.name,
      description: product.description,
      price: product.price.toString(),
      quantity: product.quantity.toString(),
    });
    setImageFile(null);
    setPreviewUrl(getImageSrc(product.imageUrl));
  };

  const resetForm = () => {
    setEditingId(null);
    setForm(emptyForm);
    setImageFile(null);
    setPreviewUrl(null);
  };

  const onSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setFormError(null);

    const body = new FormData();
    body.append("Name", form.name.trim());
    body.append("Description", form.description.trim());
    body.append("Price", form.price || "0");
    body.append("Quantity", form.quantity || "0");
    if (imageFile) {
      body.append("Image", imageFile);
    }

    try {
      if (editingId) {
        await updateProduct({ id: editingId, body }).unwrap();
      } else {
        await createProduct(body).unwrap();
      }
      resetForm();
    } catch {
      setFormError("Something went wrong while saving. Try again.");
    }
  };

  return (
    <div className="min-h-screen bg-[radial-gradient(circle_at_top,_#ffffff_0%,_#f4f1ff_40%,_#e4f3ff_85%)]">
      <main className="mx-auto flex w-full max-w-6xl flex-col gap-10 px-6 py-12">
        <header className="flex flex-col gap-6 rounded-3xl border border-white/40 bg-white/70 p-8 shadow-[0_30px_80px_-40px_rgba(15,23,42,0.45)] backdrop-blur">
          <div className="flex flex-col gap-3">
            <span className="w-fit rounded-full bg-black px-3 py-1 text-xs uppercase tracking-[0.3em] text-white">
              Product Studio
            </span>
            <h1 className="text-4xl font-semibold tracking-tight text-slate-900 sm:text-5xl">
              Manage products, inventory, and images.
            </h1>
            <p className="max-w-2xl text-base text-slate-600">
              CRUD powered by your ASP.NET API. Upload images via multipart and
              keep your catalog polished.
            </p>
          </div>
          <div className="grid gap-4 sm:grid-cols-3">
            <div className="rounded-2xl border border-slate-200 bg-white p-4">
              <p className="text-xs uppercase tracking-wide text-slate-500">
                Products
              </p>
              <p className="text-2xl font-semibold text-slate-900">
                {products.length}
              </p>
            </div>
            <div className="rounded-2xl border border-slate-200 bg-white p-4">
              <p className="text-xs uppercase tracking-wide text-slate-500">
                Units
              </p>
              <p className="text-2xl font-semibold text-slate-900">
                {stats.totalQty}
              </p>
            </div>
            <div className="rounded-2xl border border-slate-200 bg-white p-4">
              <p className="text-xs uppercase tracking-wide text-slate-500">
                Inventory value
              </p>
              <p className="text-2xl font-semibold text-slate-900">
                ${stats.totalValue.toFixed(2)}
              </p>
            </div>
          </div>
        </header>

        <section className="grid gap-8 lg:grid-cols-[1.1fr_1.9fr]">
          <form
            onSubmit={onSubmit}
            className="flex flex-col gap-5 rounded-3xl border border-slate-200 bg-white p-6 shadow-lg"
          >
            <div className="flex items-center justify-between">
              <h2 className="text-xl font-semibold text-slate-900">
                {editingId ? "Edit product" : "Add new product"}
              </h2>
              {editingId ? (
                <button
                  type="button"
                  onClick={resetForm}
                  className="rounded-full border border-slate-200 px-4 py-2 text-xs font-semibold uppercase tracking-wide text-slate-600 transition hover:border-slate-400 hover:text-slate-900"
                >
                  Cancel
                </button>
              ) : null}
            </div>

            <label className="text-sm font-medium text-slate-700">
              Name
              <input
                className="mt-2 w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-900 focus:border-slate-400 focus:outline-none"
                value={form.name}
                onChange={(event) =>
                  setForm((prev) => ({ ...prev, name: event.target.value }))
                }
                placeholder="e.g. Wireless Headphones"
                required
              />
            </label>

            <label className="text-sm font-medium text-slate-700">
              Description
              <textarea
                className="mt-2 min-h-[96px] w-full resize-none rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-900 focus:border-slate-400 focus:outline-none"
                value={form.description}
                onChange={(event) =>
                  setForm((prev) => ({
                    ...prev,
                    description: event.target.value,
                  }))
                }
                placeholder="Short product detail"
                required
              />
            </label>

            <div className="grid gap-4 sm:grid-cols-2">
              <label className="text-sm font-medium text-slate-700">
                Price
                <input
                  className="mt-2 w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-900 focus:border-slate-400 focus:outline-none"
                  value={form.price}
                  onChange={(event) =>
                    setForm((prev) => ({ ...prev, price: event.target.value }))
                  }
                  placeholder="1299.99"
                  inputMode="decimal"
                  required
                />
              </label>
              <label className="text-sm font-medium text-slate-700">
                Quantity
                <input
                  className="mt-2 w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-900 focus:border-slate-400 focus:outline-none"
                  value={form.quantity}
                  onChange={(event) =>
                    setForm((prev) => ({
                      ...prev,
                      quantity: event.target.value,
                    }))
                  }
                  placeholder="25"
                  inputMode="numeric"
                  required
                />
              </label>
            </div>

            <label className="text-sm font-medium text-slate-700">
              Image (optional)
              <input
                className="mt-2 w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600 file:mr-4 file:rounded-full file:border-0 file:bg-slate-900 file:px-4 file:py-2 file:text-xs file:uppercase file:tracking-wide file:text-white"
                type="file"
                accept="image/*"
                onChange={(event) =>
                  setImageFile(event.target.files?.[0] ?? null)
                }
              />
            </label>

            {previewUrl ? (
              <div className="overflow-hidden rounded-2xl border border-slate-200 bg-slate-100">
                <img
                  src={previewUrl}
                  alt="Preview"
                  className="h-52 w-full object-cover"
                />
              </div>
            ) : null}

            <button
              type="submit"
              disabled={isSaving}
              className="mt-2 rounded-2xl bg-slate-900 px-5 py-3 text-sm font-semibold uppercase tracking-wider text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-70"
            >
              {isSaving
                ? "Saving..."
                : editingId
                  ? "Update product"
                  : "Create product"}
            </button>

            {(formError || createStatus.isError || updateStatus.isError) && (
              <p className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
                {formError ??
                  "Something went wrong while saving. Check the API and try again."}
              </p>
            )}
          </form>

          <section className="flex flex-col gap-6">
            <div className="flex items-center justify-between">
              <h2 className="text-xl font-semibold text-slate-900">
                Catalog ({products.length})
              </h2>
              {isLoading ? (
                <span className="text-xs uppercase tracking-wide text-slate-500">
                  Loading...
                </span>
              ) : null}
            </div>

            {isError ? (
              <div className="rounded-3xl border border-amber-200 bg-amber-50 p-6 text-sm text-amber-700">
                Unable to fetch products. Make sure the API is running at{" "}
                {apiBaseUrl}.
              </div>
            ) : null}

            <div className="grid gap-4 md:grid-cols-2">
              {products.map((product) => (
                <article
                  key={product.id}
                  className="flex flex-col gap-4 rounded-3xl border border-slate-200 bg-white p-5 shadow-sm"
                >
                  {getImageSrc(product.imageUrl) ? (
                    <img
                      src={getImageSrc(product.imageUrl) ?? ""}
                      alt={product.name}
                      className="h-40 w-full rounded-2xl object-cover"
                    />
                  ) : (
                    <div className="flex h-40 items-center justify-center rounded-2xl bg-slate-100 text-xs uppercase tracking-[0.25em] text-slate-500">
                      No Image
                    </div>
                  )}
                  <div className="flex flex-col gap-2">
                    <h3 className="text-lg font-semibold text-slate-900">
                      {product.name}
                    </h3>
                    <p className="text-sm text-slate-600">
                      {product.description}
                    </p>
                    <div className="flex flex-wrap gap-3 text-xs uppercase tracking-wide text-slate-500">
                      <span>${product.price.toFixed(2)}</span>
                      <span>{product.quantity} in stock</span>
                    </div>
                  </div>
                  <div className="flex flex-wrap gap-3">
                    <button
                      type="button"
                      onClick={() => startEdit(product)}
                      className="rounded-full border border-slate-200 px-4 py-2 text-xs font-semibold uppercase tracking-wide text-slate-700 transition hover:border-slate-400 hover:text-slate-900"
                    >
                      Edit
                    </button>
                    <button
                      type="button"
                      onClick={() => deleteProduct(product.id)}
                      disabled={deleteStatus.isLoading}
                      className="rounded-full bg-rose-600 px-4 py-2 text-xs font-semibold uppercase tracking-wide text-white transition hover:bg-rose-500 disabled:cursor-not-allowed disabled:opacity-70"
                    >
                      Delete
                    </button>
                  </div>
                </article>
              ))}
            </div>
          </section>
        </section>
      </main>
    </div>
  );
}
