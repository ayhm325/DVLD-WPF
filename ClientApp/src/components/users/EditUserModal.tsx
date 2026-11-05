import { useEffect, useState, type FormEvent } from "react";
import {
  ShieldCheck,
  User as UserIcon,
  Save,
  Loader2,
  Lock,
  AlertTriangle,
  IdCard,
  Phone,
  Mail,
} from "lucide-react";
import ModalShell from "../common/ModalShell";
import PersonPhoto from "../common/PersonPhoto";
import StatusBadge from "../common/StatusBadge";
import ErrorAlert from "../common/ErrorAlert";
import LoadingSpinner from "../common/LoadingSpinner";
import { Button } from "../ui/button";
import { Input } from "../ui/input";
import { Label } from "../ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "../ui/select";
import {
  getUserDetails,
  updateUser,
  type UserDetails,
} from "../../services/userService";
import type { User } from "../../types/User";
import {
  getDeactivationBlockReason,
  getRoleChangeBlockReason,
  getRoleLabel,
  isAdminRole,
  isPrimaryAdminUser,
  validateUsername,
} from "./userValidation";

export interface EditUserModalProps {
  isOpen: boolean;
  user: User | null;
  existingUsers: User[];
  accessToken: string;
  currentUserId?: number;
  activeAdminCount: number;
  onClose: () => void;
  onUpdated: (
    updatedUserId: number,
    updatedUserName: string,
    updatedIsActive: boolean,
    updatedRole?: number,
  ) => void;
}

export default function EditUserModal({
  isOpen,
  user,
  existingUsers,
  accessToken,
  currentUserId,
  activeAdminCount,
  onClose,
  onUpdated,
}: EditUserModalProps) {
  const [details, setDetails] = useState<UserDetails | null>(null);
  const [loadingDetails, setLoadingDetails] = useState(false);

  const [userName, setUserName] = useState("");
  const [isActive, setIsActive] = useState(true);
  const [role, setRole] = useState<number>(1);
  const [confirmDeactivateChecked, setConfirmDeactivateChecked] =
    useState(false);

  const [usernameError, setUsernameError] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    let active = true;

    queueMicrotask(() => {
      if (!active) return;
      if (!isOpen || !user) {
        setDetails(null);
        setUsernameError(null);
        setSubmitError(null);
        setConfirmDeactivateChecked(false);
        return;
      }

      setUserName(user.userName);
      setIsActive(user.isActive);
      setRole(user.role);
      setUsernameError(null);
      setSubmitError(null);
      setConfirmDeactivateChecked(false);
    });

    if (!isOpen || !user || !accessToken) return;

    async function loadDetails() {
      setLoadingDetails(true);
      try {
        const data = await getUserDetails(accessToken, user!.userId);
        if (!active) return;
        setDetails(data);
      } catch {
        // Non-blocking: core User fields are already available from row data
      } finally {
        if (active) {
          setLoadingDetails(false);
        }
      }
    }

    void loadDetails();

    return () => {
      active = false;
    };
  }, [isOpen, user, accessToken]);

  if (!user) {
    return null;
  }

  const isPrimaryAdmin = isPrimaryAdminUser(user);
  const deactivationBlockReason = getDeactivationBlockReason(
    user,
    currentUserId,
    activeAdminCount,
  );
  const isTransitioningToInactive = user.isActive && !isActive;

  const roleBlockReason = getRoleChangeBlockReason(
    user,
    currentUserId,
    activeAdminCount,
    role,
  );
  const canEditRole =
    !isPrimaryAdmin &&
    (currentUserId === undefined || user.userId !== currentUserId);

  const fullName = details
    ? [
        details.firstName,
        details.secondName,
        details.thirdName,
        details.lastName,
      ]
        .filter(Boolean)
        .join(" ")
    : user.personFullName || "Linked Person";

  const hasChanges =
    userName.trim() !== user.userName ||
    isActive !== user.isActive ||
    role !== user.role;

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!user || submitting || isPrimaryAdmin) return;

    setSubmitError(null);
    setUsernameError(null);

    const formatErr = validateUsername(userName);
    if (formatErr) {
      setUsernameError(formatErr);
      return;
    }

    const trimmedUserName = userName.trim();
    const duplicate = existingUsers.some(
      (u) =>
        u.userId !== user.userId &&
        u.userName.toLowerCase() === trimmedUserName.toLowerCase(),
    );
    if (duplicate) {
      setUsernameError("Username is already in use by another user.");
      return;
    }

    if (!isActive && deactivationBlockReason) {
      setSubmitError(deactivationBlockReason);
      return;
    }

    if (role !== user.role && roleBlockReason) {
      setSubmitError(roleBlockReason);
      return;
    }

    if (isTransitioningToInactive && !confirmDeactivateChecked) {
      setSubmitError("Please confirm account deactivation before saving.");
      return;
    }

    setSubmitting(true);
    try {
      await updateUser(accessToken, user.userId, {
        personId: user.personId,
        userName: trimmedUserName,
        isActive,
        role,
      });

      onUpdated(user.userId, trimmedUserName, isActive, role);
    } catch (err) {
      setSubmitError(
        err instanceof Error ? err.message : "Failed to update user account.",
      );
    } finally {
      setSubmitting(false);
    }
  }

  const handleClose = () => {
    if (submitting) return;
    onClose();
  };

  return (
    <ModalShell
      isOpen={isOpen}
      onClose={handleClose}
      preventClose={submitting}
      size="lg"
      title={`Edit User Account · #${user.userId}`}
      description="Update permitted username, role, and account active status in accordance with DVLD security rules."
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={submitting}
            onClick={handleClose}
            className="bg-[#0e1b33] border-[#263750] text-slate-300 hover:bg-[#142647] hover:text-white cursor-pointer"
          >
            Cancel
          </Button>
          {!isPrimaryAdmin && (
            <Button
              type="submit"
              form="edit-user-form"
              size="sm"
              disabled={
                submitting ||
                !hasChanges ||
                Boolean(role !== user.role && roleBlockReason) ||
                (isTransitioningToInactive && !confirmDeactivateChecked)
              }
              className="gap-2 bg-linear-to-r from-blue-600 to-cyan-600 hover:from-blue-500 hover:to-cyan-500 text-white font-semibold shadow-md shadow-blue-600/20 cursor-pointer"
            >
              {submitting ? (
                <>
                  <Loader2 className="size-4 animate-spin" />
                  <span>Saving...</span>
                </>
              ) : (
                <>
                  <Save className="size-4" />
                  <span>Save Changes</span>
                </>
              )}
            </Button>
          )}
        </>
      }
    >
      <form
        id="edit-user-form"
        onSubmit={handleSubmit}
        className="space-y-5"
        noValidate
      >
        {submitError && <ErrorAlert message={submitError} />}

        {isPrimaryAdmin && (
          <div className="flex items-start gap-2.5 p-3.5 rounded-xl bg-amber-950/40 border border-amber-500/40 text-amber-200 text-xs leading-relaxed">
            <Lock className="size-4 text-amber-400 shrink-0 mt-0.5" />
            <div>
              <strong className="font-bold block text-amber-100">
                Protected Primary Administrator Account
              </strong>
              The primary system administrator account (
              <code>@{user.userName}</code>, ID #{user.userId}) is permanently
              protected and cannot be renamed or deactivated.
            </div>
          </div>
        )}

        {/* Linked Person Summary (Read-Only Identity) */}
        <div className="flex items-center gap-3.5 p-3.5 rounded-xl bg-[#0b1526] border border-[#223656]">
          <PersonPhoto
            personId={user.personId}
            hasPhoto={Boolean(details?.imagePath ?? user.imagePath)}
            photoKey={details?.imagePath ?? user.imagePath}
            fullName={fullName}
            accessToken={accessToken}
            size="md"
          />

          <div className="min-w-0 flex-1">
            <div className="flex items-center gap-2">
              <p className="text-sm font-bold text-white truncate">
                {fullName}
              </p>
              <StatusBadge
                variant={user.isActive ? "success" : "danger"}
                label={user.isActive ? "Active" : "Inactive"}
              />
            </div>

            {loadingDetails ? (
              <div className="mt-1">
                <LoadingSpinner
                  size="sm"
                  message="Loading person metadata..."
                />
              </div>
            ) : (
              <div className="flex flex-wrap items-center gap-2 text-xs text-slate-400 mt-1">
                <span className="font-mono">Person #{user.personId}</span>
                {details?.nationalNo && (
                  <>
                    <span>•</span>
                    <span className="inline-flex items-center gap-1 font-mono text-cyan-300">
                      <IdCard className="size-3" />
                      {details.nationalNo}
                    </span>
                  </>
                )}
                {details?.phone && (
                  <>
                    <span>•</span>
                    <span className="inline-flex items-center gap-1">
                      <Phone className="size-3" />
                      {details.phone}
                    </span>
                  </>
                )}
                {details?.email && (
                  <>
                    <span>•</span>
                    <span className="inline-flex items-center gap-1 truncate max-w-45">
                      <Mail className="size-3" />
                      {details.email}
                    </span>
                  </>
                )}
              </div>
            )}
          </div>
        </div>

        {/* Editable Account Fields */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div className="space-y-1.5">
            <Label
              htmlFor="edit-user-username"
              className="text-xs font-semibold text-slate-200"
            >
              Username <span className="text-rose-400">*</span>
            </Label>
            <Input
              id="edit-user-username"
              type="text"
              value={userName}
              disabled={submitting || isPrimaryAdmin}
              onChange={(e) => {
                setUserName(e.target.value);
                if (usernameError) setUsernameError(null);
              }}
              className="bg-[#0b1526] border-[#223656] text-white"
            />
            {usernameError ? (
              <p className="text-xs font-medium text-rose-400">
                {usernameError}
              </p>
            ) : (
              <p className="text-[11px] text-slate-400">
                3–20 chars, letters/numbers/underscores, starting with a letter.
              </p>
            )}
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="edit-user-role-select" className="text-xs font-semibold text-slate-200">
              Assigned Role {!canEditRole && "(Protected)"}
            </Label>
            {canEditRole ? (
              <Select
                value={String(role)}
                onValueChange={(val) => setRole(Number(val))}
                disabled={submitting}
              >
                <SelectTrigger
                  id="edit-user-role-select"
                  className="h-10 w-full bg-[#0b1526] border-[#223656] text-xs text-white"
                >
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="2">
                    <span className="flex items-center gap-1.5 font-semibold text-violet-300">
                      <ShieldCheck className="size-3.5 text-violet-400" />
                      Admin
                    </span>
                  </SelectItem>
                  <SelectItem value="1">
                    <span className="flex items-center gap-1.5 font-semibold text-blue-300">
                      <UserIcon className="size-3.5 text-blue-400" />
                      Staff
                    </span>
                  </SelectItem>
                </SelectContent>
              </Select>
            ) : (
              <div className="flex items-center h-10 px-3 rounded-lg bg-[#0a1324] border border-[#1e304d] justify-between">
                <span className="inline-flex items-center gap-1.5 text-xs font-bold text-white">
                  {isAdminRole(user.role) ? (
                    <ShieldCheck className="size-4 text-violet-400" />
                  ) : (
                    <UserIcon className="size-4 text-blue-400" />
                  )}
                  {getRoleLabel(user.role)}
                </span>
                <span className="text-[10px] uppercase tracking-wider font-semibold text-amber-400">
                  {isPrimaryAdmin ? "Primary Admin" : "Self Account"}
                </span>
              </div>
            )}
            {roleBlockReason ? (
              <p className="flex items-center gap-1.5 text-xs text-amber-300 font-medium">
                <Lock className="size-3.5 shrink-0" />
                <span>{roleBlockReason}</span>
              </p>
            ) : (
              <p className="text-[11px] text-slate-400">
                {canEditRole
                  ? "Admin accounts have full user management; Staff have operational access."
                  : isPrimaryAdmin
                    ? "The primary system administrator role cannot be altered."
                    : "You cannot change your own role to prevent accidental lockout."}
              </p>
            )}
          </div>
        </div>

        {/* Account Active Status Control */}
        <div className="space-y-2">
          <Label
            htmlFor="edit-user-status"
            className="text-xs font-semibold text-slate-200"
          >
            Account Status
          </Label>
          <Select
            value={isActive ? "active" : "inactive"}
            onValueChange={(val) => {
              const nextActive = val === "active";
              setIsActive(nextActive);
              if (nextActive) {
                setConfirmDeactivateChecked(false);
              }
            }}
            disabled={submitting || Boolean(deactivationBlockReason)}
          >
            <SelectTrigger
              id="edit-user-status"
              className="h-10 w-full bg-[#0b1526] border-[#223656] text-xs text-white"
            >
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="active">
                Active — Authorized to sign in
              </SelectItem>
              <SelectItem value="inactive">
                Inactive — Sign-in & active tokens revoked
              </SelectItem>
            </SelectContent>
          </Select>

          {deactivationBlockReason && (
            <p className="flex items-center gap-1.5 text-xs text-amber-300 font-medium">
              <Lock className="size-3.5 shrink-0" />
              <span>{deactivationBlockReason}</span>
            </p>
          )}
        </div>

        {/* Explicit Deactivation Confirmation Safeguard */}
        {isTransitioningToInactive && !deactivationBlockReason && (
          <div className="p-3.5 rounded-xl bg-rose-950/35 border border-rose-500/40 space-y-2.5">
            <div className="flex items-start gap-2.5 text-xs text-rose-200">
              <AlertTriangle className="size-4 text-rose-400 shrink-0 mt-0.5" />
              <div>
                <strong className="font-bold block text-rose-100">
                  Deactivation Confirmation Required
                </strong>
                Deactivating <strong>@{user.userName}</strong> will immediately
                invalidate any active session tokens and prevent this user from
                signing in until reactivated.
              </div>
            </div>

            <label className="flex items-center gap-2 text-xs font-semibold text-white cursor-pointer select-none pt-1">
              <input
                type="checkbox"
                checked={confirmDeactivateChecked}
                onChange={(e) => setConfirmDeactivateChecked(e.target.checked)}
                className="rounded border-rose-400 bg-slate-900 text-rose-500 focus:ring-rose-500"
              />
              <span>
                I confirm deactivating @{user.userName}&apos;s account
              </span>
            </label>
          </div>
        )}
      </form>
    </ModalShell>
  );
}
