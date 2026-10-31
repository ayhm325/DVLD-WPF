export type ApplicationType = {
  applicationTypeId: number
  applicationTypeTitle: string
  applicationTypeFees: number
}

export type UpdateApplicationTypeRequest = {
  applicationTypeTitle: string
  applicationTypeFees: number
}
