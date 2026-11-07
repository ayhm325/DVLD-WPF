export type TestAppointment = {
  testAppointmentId: number
  testTypeId: number
  testTypeName: string
  localDrivingLicenseApplicationId: number
  applicantFullName?: string
  nationalNo?: string
  licenseClassName?: string
  appointmentDate: string
  appointmentDateFormatted?: string
  paidFees: number
  createdByUserId: number
  createdByUserName: string
  isLocked: boolean
  retakeTestApplicationId?: number | null
  testResult: number
  testResultText: string
  status: string
  cancelledAt?: string | null
  cancelledByUserId?: number | null
  noShowDate?: string | null
}

export type ScheduleTestPreparation = {
  appointmentId: number
  retakeTestApplicationId: number | null
  localDrivingLicenseApplicationId: number
  licenseClassName: string
  fullName: string
  trial: number
  date: string
  fees: number
  testTypeId: number
  retakerFees: number
  testId: number
  result: boolean
  notes: string | null
}

export type ScheduleTestPayload = {
  localDrivingLicenseApplicationId: number
  testTypeId: number
  appointmentDate: string
}

export type AppointmentSlot = {
  startTime: string
  endTime: string
}

export type AppointmentDayAvailability = {
  date: string
  slots: AppointmentSlot[]
}

export type AppointmentAvailabilityResponse = {
  localAppId: number
  testTypeId: number
  availableDays: AppointmentDayAvailability[]
}

export type TestResponse = {
  testId: number
  testAppointmentId: number
  testResult: boolean
  notes: string | null
  createdByUserId: number
  createdByUserName: string | null
  testTypeName: string | null
  appointmentDate: string | null
}

export type SaveTestResultPayload = {
  testAppointmentId: number
  testResult: boolean
  notes: string | null
}

export type ExaminationAttemptStats = {
  localDrivingLicenseApplicationId: number
  testTypeId: number
  totalAttempts: number
  passedAttempts: number
  failedAttempts: number
  noShowAttempts: number
  nextTrialNumber: number
}
