
import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
    ArrowRight,
    Edit3,
    Mail,
    Plus,
    Search,
    ShieldCheck,
    Trash2,
    Users,
    X
} from "lucide-react";

import { api, errorMessage } from "../api/client";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";

const blank = {
    name: "",
    email: "",
    password: "student123",
    role: "Student",
    phone: "",
    department: "",
    expertise: ""
};

// Return a form with the role matching the current page.
// The All Users page defaults to Student, allowing the admin to change it.
function blankForType(type) {
    return {
        ...blank,
        role:
            type === "Student" || type === "Trainer"
                ? type
                : "Student"
    };
}

export default function PeoplePage({ type }) {
    const { user } = useAuth();
    const notify = useToast();

    const [users, setUsers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [search, setSearch] = useState("");
    const [formOpen, setFormOpen] = useState(false);
    const [editing, setEditing] = useState(null);
    const [form, setForm] = useState(() => blankForType(type));
    const [saving, setSaving] = useState(false);

    const admin = user?.role === "Admin";

    const heading =
        type === "Student"
            ? "Student community"
            : type === "Trainer"
                ? "Teaching team"
                : "User management";

    const subheading =
        type === "Student"
            ? "Learners enrolled across your institute."
            : type === "Trainer"
                ? "Your institute’s dedicated teaching team."
                : "Manage learner, trainer and administrator accounts.";

    const load = useCallback(async () => {
        setLoading(true);

        try {
            const path =
                type === "Student"
                    ? "/students"
                    : type === "Trainer"
                        ? "/trainers"
                        : `/users${search
                            ? `?search=${encodeURIComponent(search)}`
                            : ""
                        }`;

            const { data } = await api.get(path);
            setUsers(data);
        } catch (err) {
            notify(errorMessage(err), "error");
        } finally {
            setLoading(false);
        }
    }, [type, search, notify]);

    useEffect(() => {
        const timer = setTimeout(load, 120);

        return () => clearTimeout(timer);
    }, [load]);

    function startCreate() {
        setEditing(null);
        setForm(blankForType(type));
        setFormOpen(true);
    }

    async function save(e) {
        e.preventDefault();
        setSaving(true);

        try {
            if (editing) {
                const payload = {
                    name: form.name,
                    email: form.email,
                    phone: form.phone || null,
                    expertise: form.expertise || null,
                    department: form.department || null,
                    isActive: true
                };

                if (form.password) {
                    payload.password = form.password;
                }

                await api.put(`/users/${editing.id}`, payload);

                notify("User profile updated.", "success");
            } else {
                // Enforce the role for dedicated Student/Trainer pages.
                // The All Users page uses the role selected in its dropdown.
                const createPayload = {
                    ...form,
                    role:
                        type === "Student" || type === "Trainer"
                            ? type
                            : form.role
                };

                await api.post("/users", createPayload);

                notify(
                    `${createPayload.role} account created.`,
                    "success"
                );
            }

            setFormOpen(false);
            setEditing(null);
            setForm(blankForType(type));

            await load();
        } catch (err) {
            notify(errorMessage(err), "error");
        } finally {
            setSaving(false);
        }
    }

    function startEdit(person) {
        setEditing(person);

        setForm({
            ...blank,
            name: person.name,
            email: person.email,
            role: person.role,
            phone: person.phone || "",
            department: person.department || "",
            expertise: person.expertise || "",
            password: ""
        });

        setFormOpen(true);
    }

    async function remove(person) {
        if (
            !window.confirm(
                `Delete ${person.name}'s account? This removes their learning records.`
            )
        ) {
            return;
        }

        try {
            await api.delete(`/users/${person.id}`);

            notify(`${person.name} deleted.`, "info");

            await load();
        } catch (err) {
            notify(errorMessage(err), "error");
        }
    }

    const visible = users.filter((u) =>
        `${u.name} ${u.email} ${u.expertise || ""} ${u.department || ""}`
            .toLowerCase()
            .includes(search.toLowerCase())
    );

    return (
        <div className="page-stack">
            <div className="people-toolbar">
                <div className="people-search panel">
                    <Search size={17} />

                    <input
                        placeholder={`Search ${type === "All" ? "users" : `${type.toLowerCase()}s`
                            } by name or email…`}
                        value={search}
                        onChange={(e) => setSearch(e.target.value)}
                    />
                </div>

                {admin && (
                    <button
                        type="button"
                        className="btn btn-primary"
                        onClick={startCreate}
                    >
                        <Plus size={17} />
                        Add {type === "All" ? "user" : type.toLowerCase()}
                    </button>
                )}
            </div>

            <div className="section-title-row">
                <div>
                    <h2>{heading}</h2>
                    <p>{subheading}</p>
                </div>

                <span className="view-chip">
                    <Users size={15} />
                    {visible.length} people
                </span>
            </div>

            {formOpen && (
                <div
                    className="modal-backdrop"
                    onMouseDown={(e) => {
                        if (e.target === e.currentTarget) {
                            setFormOpen(false);
                        }
                    }}
                >
                    <form className="modal-card" onSubmit={save}>
                        <div className="modal-heading">
                            <div>
                                <span className="eyebrow">
                                    {editing
                                        ? "PROFILE SETTINGS"
                                        : "NEW ACCOUNT"}
                                </span>

                                <h2>
                                    {editing
                                        ? "Edit user"
                                        : "Create an account"}
                                </h2>

                                <p>
                                    {editing
                                        ? "Update contact details and profile information."
                                        : "Create a new account for your institute."}
                                </p>
                            </div>

                            <button
                                type="button"
                                className="icon-btn"
                                onClick={() => setFormOpen(false)}
                            >
                                <X size={19} />
                            </button>
                        </div>

                        <div className="form-grid">
                            <Field label="Full name">
                                <input
                                    required
                                    maxLength={100}
                                    value={form.name}
                                    onChange={(e) =>
                                        setForm({
                                            ...form,
                                            name: e.target.value
                                        })
                                    }
                                />
                            </Field>

                            <Field label="Email address">
                                <input
                                    required
                                    type="email"
                                    value={form.email}
                                    onChange={(e) =>
                                        setForm({
                                            ...form,
                                            email: e.target.value
                                        })
                                    }
                                />
                            </Field>

                            {!editing && (
                                <Field label="Password">
                                    <input
                                        required
                                        minLength={8}
                                        type="password"
                                        value={form.password}
                                        onChange={(e) =>
                                            setForm({
                                                ...form,
                                                password: e.target.value
                                            })
                                        }
                                    />
                                </Field>
                            )}

                            {editing && (
                                <Field label="New password (optional)">
                                    <input
                                        minLength={8}
                                        type="password"
                                        placeholder="Leave blank to keep current"
                                        value={form.password}
                                        onChange={(e) =>
                                            setForm({
                                                ...form,
                                                password: e.target.value
                                            })
                                        }
                                    />
                                </Field>
                            )}

                            <Field label="Role">
                                <select
                                    disabled={!!editing || type !== "All"}
                                    value={form.role}
                                    onChange={(e) =>
                                        setForm({
                                            ...form,
                                            role: e.target.value
                                        })
                                    }
                                >
                                    {(type === "All"
                                        ? ["Student", "Trainer", "Admin"]
                                        : [type]
                                    ).map((r) => (
                                        <option key={r} value={r}>
                                            {r}
                                        </option>
                                    ))}
                                </select>
                            </Field>

                            <Field label="Phone">
                                <input
                                    maxLength={30}
                                    value={form.phone}
                                    onChange={(e) =>
                                        setForm({
                                            ...form,
                                            phone: e.target.value
                                        })
                                    }
                                />
                            </Field>

                            {form.role === "Student" && (
                                <Field label="Department" span>
                                    <input
                                        value={form.department}
                                        onChange={(e) =>
                                            setForm({
                                                ...form,
                                                department: e.target.value
                                            })
                                        }
                                        placeholder="Computer Science"
                                    />
                                </Field>
                            )}

                            {form.role === "Trainer" && (
                                <Field label="Expertise" span>
                                    <input
                                        value={form.expertise}
                                        onChange={(e) =>
                                            setForm({
                                                ...form,
                                                expertise: e.target.value
                                            })
                                        }
                                        placeholder="React, ASP.NET Core, SQL Server"
                                    />
                                </Field>
                            )}
                        </div>

                        <div className="modal-actions">
                            <button
                                type="button"
                                className="btn btn-ghost"
                                onClick={() => setFormOpen(false)}
                            >
                                Cancel
                            </button>

                            <button
                                type="submit"
                                disabled={saving}
                                className="btn btn-primary"
                            >
                                {saving
                                    ? "Saving…"
                                    : editing
                                        ? "Save changes"
                                        : "Create account"}

                                <ArrowRight size={15} />
                            </button>
                        </div>
                    </form>
                </div>
            )}

            {loading ? (
                <div className="people-card-grid">
                    {[1, 2, 3, 4, 5, 6].map((n) => (
                        <div className="person-skeleton" key={n}>
                            <span />
                            <span />
                            <span />
                        </div>
                    ))}
                </div>
            ) : visible.length === 0 ? (
                <div className="panel empty-results">
                    <div className="empty-state-icon">
                        <Users size={22} />
                    </div>

                    <h3>No people found</h3>

                    <p>
                        Try adjusting the search, or create a new user.
                    </p>

                    {admin && (
                        <button
                            type="button"
                            className="btn btn-primary"
                            onClick={startCreate}
                        >
                            <Plus size={15} />
                            Add user
                        </button>
                    )}
                </div>
            ) : (
                <div className="people-card-grid">
                    {visible.map((person, i) => (
                        <article
                            className="person-card panel"
                            key={person.id}
                        >
                            <div className="person-card-top">
                                <div
                                    className={`person-avatar ${i % 3 === 0
                                            ? "person-avatar-purple"
                                            : i % 3 === 1
                                                ? "person-avatar-blue"
                                                : "person-avatar-green"
                                        }`}
                                >
                                    {person.name
                                        ?.charAt(0)
                                        ?.toUpperCase()}
                                </div>

                                <span
                                    className={`role-badge role-${person.role?.toLowerCase()}`}
                                >
                                    {person.role}
                                </span>

                                <button
                                    type="button"
                                    className="person-more"
                                    title="Account actions"
                                    onClick={() => {
                                        if (admin) startEdit(person);
                                    }}
                                >
                                    <Edit3 size={16} />
                                </button>
                            </div>

                            <h3>{person.name}</h3>

                            <div className="person-email">
                                <Mail size={14} />
                                {person.email}
                            </div>

                            {person.expertise && (
                                <div className="person-expertise">
                                    {person.expertise}
                                </div>
                            )}

                            {person.department && (
                                <div className="person-expertise">
                                    {person.department}
                                </div>
                            )}

                            <div className="person-card-foot">
                                <span>
                                    <ShieldCheck size={14} />
                                    Active account
                                </span>

                                <div className="person-actions">
                                    {person.role === "Student" && (
                                        <Link
                                            to={`/marks?studentUserId=${person.id}`}
                                            className="person-action-link"
                                        >
                                            Marks
                                            <ArrowRight size={13} />
                                        </Link>
                                    )}

                                    {person.role === "Student" && (
                                        <Link
                                            to={`/dashboard?studentUserId=${person.id}`}
                                            className="person-action-link"
                                        >
                                            Progress
                                            <ArrowRight size={13} />
                                        </Link>
                                    )}

                                    {admin && (
                                        <button
                                            type="button"
                                            className="person-delete"
                                            onClick={() => remove(person)}
                                            title="Delete user"
                                        >
                                            <Trash2 size={15} />
                                        </button>
                                    )}
                                </div>
                            </div>
                        </article>
                    ))}
                </div>
            )}
        </div>
    );
}

function Field({ label, children, span }) {
    return (
        <label
            className={`form-field ${span ? "form-span-2" : ""}`}
        >
            <span>{label}</span>
            {children}
        </label>
    );
}
