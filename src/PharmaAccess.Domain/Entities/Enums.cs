namespace PharmaAccess.Domain.Entities;

public enum DatasetVersionStatus { Draft, Validating, Validated, Rejected, Finalized, Archived }
public enum DatasetValidationStatus { NotValidated, InProgress, Passed, Failed }
public enum SourceType { FDAFirstGeneric, MedicaidStateDrugUtilization, RxNorm, StateReference, Other }
public enum SourceFileImportStatus { Registered, Imported, Rejected }
public enum DataQualityStatus { Unchecked, Valid, Warning, Error, Blocking }
public enum JobRunStatus { Pending, Running, Succeeded, Failed, Cancelled }

