
import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
    Award,
    ArrowRight,
    BadgeCheck,
    Download,
    ExternalLink,
    Plus,
    ShieldCheck,
    Sparkles
} from "lucide-react";
import html2canvas from "html2canvas";
import { jsPDF } from "jspdf";

import { api, errorMessage } from "../api/client";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";

export default function CertificatesPage() {
    const { user } = useAuth();
    const notify = useToast();

    const [certificates, setCertificates] = useState([]);
    const [progress, setProgress] = useState([]);
    const [courses, setCourses] = useState([]);
    const [students, setStudents] = useState([]);
    const [issue, setIssue] = useState({
        courseId: "",
        studentUserId: ""
    });
    const [loading, setLoading] = useState(true);
    const [downloadingId, setDownloadingId] = useState(null);

    const staff = ["Admin", "Trainer"].includes(user?.role);

    const load = useCallback(async () => {
        setLoading(true);

        try {
            if (staff) {
                const [certs, courseList, studentList] =
                    await Promise.all([
                        api.get("/certificates"),
                        api.get("/courses"),
                        api.get("/students")
                    ]);

                setCertificates(certs.data);
                setCourses(courseList.data);
                setStudents(studentList.data);
            } else {
                const [certs, studentProgress] = await Promise.all([
                    api.get("/certificates"),
                    api.get("/progress")
                ]);

                setCertificates(certs.data);
                setProgress(studentProgress.data);
            }
        } catch (err) {
            notify(errorMessage(err), "error");
        } finally {
            setLoading(false);
        }
    }, [staff, notify]);

    useEffect(() => {
        load();
    }, [load]);

    async function issueCertificate(courseId, studentUserId) {
        try {
            await api.post("/certificates", {
                courseId: Number(courseId),
                studentUserId: studentUserId
                    ? Number(studentUserId)
                    : null
            });

            notify("Certificate issued successfully.", "success");

            setIssue({
                courseId: "",
                studentUserId: ""
            });

            await load();
        } catch (err) {
            notify(errorMessage(err), "error");
        }
    }

    // Download only the selected certificate as a PDF.
    async function downloadCertificate(cert) {
        const elementId = `certificate-card-${cert.id}`;
        const certificateElement = document.getElementById(elementId);

        if (!certificateElement) {
            notify("Certificate element not found.", "error");
            return;
        }

        setDownloadingId(cert.id);

        try {
            const canvas = await html2canvas(certificateElement, {
                scale: 3,
                useCORS: true,
                backgroundColor: "#ffffff",
                logging: false,

                // Remove the action buttons from the exported PDF.
                onclone: (clonedDocument) => {
                    const clonedCard =
                        clonedDocument.getElementById(elementId);

                    if (clonedCard) {
                        clonedCard
                            .querySelector(".certificate-actions")
                            ?.remove();

                        clonedCard.style.boxShadow = "none";
                        clonedCard.style.margin = "0";
                    }
                }
            });

            const imageData = canvas.toDataURL("image/png");
            const landscape = canvas.width >= canvas.height;

            // Match the PDF page dimensions to the certificate's shape.
            const pdfWidth = landscape ? 280 : 200;
            const pdfHeight = pdfWidth * (canvas.height / canvas.width);

            const pdf = new jsPDF({
                orientation: landscape ? "landscape" : "portrait",
                unit: "mm",
                format: [pdfWidth, pdfHeight],
                compress: true
            });

            pdf.addImage(
                imageData,
                "PNG",
                0,
                0,
                pdfWidth,
                pdfHeight,
                undefined,
                "FAST"
            );

            function safeFilename(value) {
                return String(value || "certificate")
                    .normalize("NFKD")
                    .replace(/[\u0300-\u036f]/g, "")
                    .replace(/[^a-zA-Z0-9_-]+/g, "-")
                    .replace(/^-+|-+$/g, "")
                    .toLowerCase();
            }

            const filename =
                `${safeFilename(cert.studentName)}-` +
                `${safeFilename(cert.courseTitle)}-certificate.pdf`;

            pdf.save(filename);

            notify("Certificate downloaded successfully.", "success");
        } catch (err) {
            console.error("Certificate download failed:", err);
            notify(
                "Unable to download the certificate. Please try again.",
                "error"
            );
        } finally {
            setDownloadingId(null);
        }
    }

    const eligibleProgress = progress.filter(
        (p) => p.certificateEligible && !p.certificateCode
    );

    return (
        <div className="page-stack">
            <section className="certificate-banner">
                <div className="certificate-banner-icon">
                    <Award size={28} />
                </div>

                <div>
                    <span className="eyebrow eyebrow-light">
                        YOUR ACHIEVEMENTS
                    </span>
                    <h2>Proof of your progress.</h2>
                    <p>
                        Every certificate is uniquely numbered and can be
                        verified online by employers.
                    </p>
                </div>

                <div className="certificate-count">
                    <b>{certificates.length}</b>
                    <span>certificates issued</span>
                </div>
            </section>

            {!staff && eligibleProgress.length > 0 && (
                <section className="panel eligible-panel">
                    <div className="panel-heading">
                        <div>
                            <h3>Ready to claim</h3>
                            <p>
                                You have completed every lesson and assignment
                                and passed a quiz.
                            </p>
                        </div>

                        <span className="panel-icon">
                            <Sparkles size={18} />
                        </span>
                    </div>

                    {eligibleProgress.map((p) => (
                        <div className="eligible-row" key={p.courseId}>
                            <div className="eligible-course">
                                <span className="eligible-icon">
                                    <BadgeCheck size={18} />
                                </span>

                                <span>
                                    <b>{p.courseTitle}</b>
                                    <small>Completion requirements met</small>
                                </span>
                            </div>

                            <button
                                type="button"
                                className="btn btn-success btn-small"
                                onClick={() =>
                                    issueCertificate(p.courseId, null)
                                }
                            >
                                Claim certificate
                                <ArrowRight size={14} />
                            </button>
                        </div>
                    ))}
                </section>
            )}

            {staff && (
                <section className="panel issue-panel">
                    <div className="panel-heading">
                        <div>
                            <h3>Issue a certificate</h3>
                            <p>
                                Admin and trainers can issue a verified
                                certificate to an enrolled student.
                            </p>
                        </div>

                        <span className="panel-icon">
                            <ShieldCheck size={18} />
                        </span>
                    </div>

                    <div className="issue-form">
                        <label className="form-field">
                            <span>Course</span>
                            <select
                                value={issue.courseId}
                                onChange={(e) =>
                                    setIssue({
                                        ...issue,
                                        courseId: e.target.value
                                    })
                                }
                            >
                                <option value="">Select course…</option>
                                {courses.map((course) => (
                                    <option
                                        key={course.id}
                                        value={course.id}
                                    >
                                        {course.title}
                                    </option>
                                ))}
                            </select>
                        </label>

                        <label className="form-field">
                            <span>Student</span>
                            <select
                                value={issue.studentUserId}
                                onChange={(e) =>
                                    setIssue({
                                        ...issue,
                                        studentUserId: e.target.value
                                    })
                                }
                            >
                                <option value="">Select student…</option>
                                {students.map((student) => (
                                    <option
                                        key={student.id}
                                        value={student.id}
                                    >
                                        {student.name} · {student.email}
                                    </option>
                                ))}
                            </select>
                        </label>

                        <button
                            type="button"
                            disabled={
                                !issue.courseId || !issue.studentUserId
                            }
                            className="btn btn-primary btn-small"
                            onClick={() =>
                                issueCertificate(
                                    issue.courseId,
                                    issue.studentUserId
                                )
                            }
                        >
                            <Plus size={15} />
                            Issue certificate
                        </button>
                    </div>
                </section>
            )}

            <div className="section-title-row">
                <div>
                    <h2>
                        {staff ? "Issued credentials" : "Your certificates"}
                    </h2>
                    <p>
                        {staff
                            ? "All verified certificates issued by the institute."
                            : "A record of every course you’ve completed."}
                    </p>
                </div>
            </div>

            {loading ? (
                <div className="certificate-grid">
                    {[1, 2].map((i) => (
                        <div className="cert-skeleton" key={i} />
                    ))}
                </div>
            ) : certificates.length > 0 ? (
                <div className="certificate-grid">
                    {certificates.map((cert) => (
                        <article
                            className="certificate-card"
                            id={`certificate-card-${cert.id}`}
                            key={cert.id}
                        >
                            <div className="certificate-pattern" />

                            <div className="certificate-inner">
                                <div className="certificate-emblem">
                                    <GraduationCapIcon />
                                </div>

                                <div className="certificate-kicker">
                                    CERTIFICATE OF COMPLETION
                                </div>

                                <div className="certificate-award">
                                    This certificate is proudly presented to
                                </div>

                                <h3>{cert.studentName}</h3>

                                <div className="certificate-award">
                                    for successfully completing
                                </div>

                                <h4>{cert.courseTitle}</h4>

                                <p className="certificate-completion">
                                    with a final score of{" "}
                                    <b>{Math.round(cert.finalScore)}%</b>
                                </p>

                                <div className="certificate-signature">
                                    <span className="signature-line" />
                                    <small>Learnwise · Academic Office</small>
                                </div>

                                <div className="certificate-bottom">
                                    <span>
                                        No. {cert.certificateNumber}
                                    </span>
                                    <span>
                                        Issued{" "}
                                        {new Date(
                                            cert.issueDateUtc
                                        ).toLocaleDateString()}
                                    </span>
                                </div>

                                <div className="certificate-verify">
                                    <span className="verify-dot">
                                        <CheckIcon />
                                    </span>
                                    <span>Verified credential</span>
                                    <b>{cert.verificationCode}</b>
                                </div>
                            </div>

                            <div className="certificate-actions">
                                <button
                                    type="button"
                                    className="btn btn-outline btn-small"
                                    disabled={downloadingId === cert.id}
                                    onClick={() =>
                                        downloadCertificate(cert)
                                    }
                                >
                                    <Download size={14} />
                                    {downloadingId === cert.id
                                        ? "Preparing PDF..."
                                        : "Download PDF"}
                                </button>

                                <Link
                                    to={`/verify?code=${cert.verificationCode}`}
                                    className="btn btn-ghost btn-small"
                                >
                                    Verify
                                    <ExternalLink size={14} />
                                </Link>
                            </div>
                        </article>
                    ))}
                </div>
            ) : (
                <div className="panel empty-results">
                    <div className="empty-state-icon">
                        <Award size={22} />
                    </div>

                    <h3>No certificates yet</h3>
                    <p>
                        Complete all course lessons and assignments and pass
                        a quiz to qualify.
                    </p>

                    <Link to="/courses" className="btn btn-primary">
                        Explore courses
                        <ArrowRight size={15} />
                    </Link>
                </div>
            )}
        </div>
    );
}

function GraduationCapIcon() {
    return <Award size={23} />;
}

function CheckIcon() {
    return <ShieldCheck size={14} />;
}
