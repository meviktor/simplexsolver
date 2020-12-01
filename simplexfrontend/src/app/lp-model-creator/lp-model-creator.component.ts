import { Component, OnInit } from '@angular/core';
import { LpsolverService } from '../_services/lpsolver.service';

@Component({
  selector: 'app-lp-model-creator',
  templateUrl: './lp-model-creator.component.html',
  styleUrls: ['./lp-model-creator.component.css']
})
export class LpModelCreatorComponent implements OnInit {
  readonly NOT_INTEGER_ERROR_MSG = "You have to provide an integer! Position of the error: ";

  readonly MIN_NUM_DECISIONVARS = 2;
  readonly MAX_NUM_DECISIONVARS = 10;
  readonly MIN_NUM_CONSTRAINTS = 1;
  readonly MAX_NUM_CONSTRAINTS = 10;

  readonly decisionVariableName: string = "x";
  readonly functionVariableName: string = "z";
  readonly firstPhaseFunctionVariableName: string = "w";

  readonly constraintAttributeName: string = "constraintNo";
  readonly variableAttributeName: string = "variableNo";
  readonly objectiveFunctionClassName: string = "objectiveFunction";
  readonly interpretationRangeClassName: string = "interpretationRange";
  readonly sideConnectionClassName: string = "sideConnection";
  readonly optimizationAimSelectId: string = "optimizationAim";
  readonly possibleSideConnections: string[] = ["<=", "=", ">="];
  readonly possibleSides: string[] = ["leftSide", "rightSide"];

  numberOfDecisionVariables: number = 0;
  numberOfConstraints: number = 0;

  constraintsLeftSideMatrix: number[][] = [];
  constraintConnectionsVector: number[] = [];
  constraintsRightVector: number[] = [];
  interpretationRangeVector: (number | null)[] = [];

  maximization: boolean = true;
  objectiveCoefficientVector: number[] = [];

  constructor(private lpSolverService: LpsolverService) { }

  ngOnInit(): void {
    this.hideElementById("constraintsObjectiveInterpretationContainer");
  }

  public sendLPModelToSolver(): void{
    var requestObject;
    var successfullyValidated = true;
    var errorMessage;

    try{
      requestObject = this.composeLPModelRequestJsonFromView();
    }
    catch(e){
      successfullyValidated = false;
      errorMessage = e.message;
    }

    if(successfullyValidated){
      this.lpSolverService.solveLP(requestObject)
        .subscribe(solution => console.log(solution));
    }
    else{
      alert(errorMessage);
    }
  }

  numberDecisionVariablesAndConstraintsChanged(): void {
    var numOfVars = Number((document.getElementById("numDecisionVars") as HTMLInputElement).value);
    var numOfConstraints = Number((document.getElementById("numConstraints") as HTMLInputElement).value);

    if(!(numOfVars % 1 == 0 && numOfVars >= this.MIN_NUM_DECISIONVARS && numOfVars <= this.MAX_NUM_DECISIONVARS) ||
       !(numOfConstraints % 1 == 0 && numOfConstraints >= this.MIN_NUM_CONSTRAINTS && numOfConstraints <= this.MAX_NUM_CONSTRAINTS)){
        alert("Check the entered values! Allowed range for the number of decision variables: from 2 to 10. Allowed range for constraints: from 1 to 10.");
        return;
    }

    this.numberOfDecisionVariables = numOfVars;
    this.numberOfConstraints = numOfConstraints;

    this.reinitializeConstraintsAndObjective();

    var constraintsGridHtml = this.buildConstraintsGrid();
    this.setConstraintsContainerContent(constraintsGridHtml);

    var objectiveFunctionGridHtml = this.buildObjectiveFunctionGrid();
    this.setObjectiveContainerContent(objectiveFunctionGridHtml);

    var interpretationRangesGridHtml = this.buildInterpretationRangesGrid();
    this.setInterpretationRangesContainerContent(interpretationRangesGridHtml);

    this.showElementById("constraintsObjectiveInterpretationContainer");
  }

  private reinitializeConstraintsAndObjective(): void {
    this.constraintsLeftSideMatrix = [];
    this.constraintConnectionsVector = [];
    this.constraintsRightVector = [];
    this.interpretationRangeVector = [];

    this.maximization = true;
    this.objectiveCoefficientVector = [];
  }

  private buildConstraintsGrid(): string {
    var htmlContent = "";

    // row - constraint
    for(var constraintNo = 1; constraintNo <= this.numberOfConstraints; ++constraintNo){
      htmlContent += `<div class="row">`;

      // column - decision variable coefficients
      for(var variableNo = 1; variableNo <= this.numberOfDecisionVariables; ++variableNo){
        htmlContent += 
        `<div class="col-xl-1 col-lg-1 col-md-2 col-sm-4 xs-6">
          <div class="form-group">
            <label>${this.decisionVariableName}${variableNo}:</label>
            <input type="number" class="${this.possibleSides[0]}" ${this.constraintAttributeName}="${constraintNo}" ${this.variableAttributeName}="${variableNo}"/>
          </div>
        </div>`
      }

      // column - sideConnection: relation of left and right side
      htmlContent += 
      `<div class="col-xl-1 col-lg-1 col-md-2 col-sm-4 xs-6">
        <div class="form-group">
          <label>Relation:</label>
          <select class="${this.sideConnectionClassName}" ${this.constraintAttributeName}="${constraintNo}">
            <option value="${this.possibleSideConnections[0]}">${this.possibleSideConnections[0]}</option>
            <option value="${this.possibleSideConnections[1]}">${this.possibleSideConnections[1]}</option>
            <option value="${this.possibleSideConnections[2]}">${this.possibleSideConnections[2]}</option>
          </select>
        </div>
      </div>`;

      // column - right side constant
      htmlContent += 
        `<div class="col-xl-1 col-lg-1 col-md-2 col-sm-4 xs-6">
          <div class="form-group">
            <label></label>
            <input type="number" class="${this.possibleSides[1]}" ${this.constraintAttributeName}="${constraintNo}"/>
          </div>
        </div>`;

      htmlContent += `</div>`;
    }

    return htmlContent;
  }

  private buildObjectiveFunctionGrid(): string{
    var htmlContent = "";

    htmlContent += `<div class="row">`;
    // decision variable coefficients
    for(var variableNo = 1; variableNo <= this.numberOfDecisionVariables; ++variableNo){
      htmlContent += 
      `<div class="col-xl-1 col-lg-1 col-md-2 col-sm-4 xs-6">
        <div class="form-group">
          <label>${this.decisionVariableName}${variableNo}:</label>
          <input type="number" class="${this.objectiveFunctionClassName}" ${this.variableAttributeName}="${variableNo}"/>
        </div>
      </div>`
    }

    // optimization aim + placeholder
    htmlContent += 
    `<div class="col-xl-1 col-lg-1 col-md-2 col-sm-4 xs-6">
      <div class="form-group">
        <label>Aim:</label>
        <select id="${this.optimizationAimSelectId}">
          <option value=true>Max.</option>
          <option value=false>Min.</option>
        </select>
      </div>
    </div>`;
    htmlContent += `<div class="col-2"></div>`
    htmlContent += `</div>`;

    return htmlContent;
  }

  private buildInterpretationRangesGrid(): string {
    var htmlContent = "";

    htmlContent += `<div class="row">`;
    // decision variable coefficients
    for(var variableNo = 1; variableNo <= this.numberOfDecisionVariables; ++variableNo){
      htmlContent += 
      `<div class="col">
        <div class="form-group">
          <label>${this.decisionVariableName}${variableNo}:</label>
          <input type="number" class="${this.interpretationRangeClassName}" ${this.variableAttributeName}="${variableNo}"/>
        </div>
      </div>`
    }
    htmlContent += `</div>`;

    return htmlContent;
  }

  private setConstraintsContainerContent(htmlContent: string){
    var grid = document.getElementById("constraintsContainer") as HTMLElement;
    grid.innerHTML = htmlContent;
  }

  private setObjectiveContainerContent(htmlContent: string){
    var grid = document.getElementById("objectiveContainer") as HTMLElement;
    grid.innerHTML = htmlContent;
  }

  private setInterpretationRangesContainerContent(htmlContent: string){
    var grid = document.getElementById("interpretationRangesContainer") as HTMLElement;
    grid.innerHTML = htmlContent;
  }

  private composeLPModelRequestJsonFromView(): object {
    this.loadLPModel();

    var requestObject = {
      decisionVariableName: this.decisionVariableName,
      functionVariableName: this.functionVariableName,
      firstPhaseFunctionVariableName: this.firstPhaseFunctionVariableName,
      numberOfDecisionVariables: this.numberOfDecisionVariables,
      numberOfConstraints: this.numberOfConstraints,
      constraintsLeftSideMatrix: this.constraintsLeftSideMatrix,
      constraintConnectionsVector: this.constraintConnectionsVector,
      constraintsRightVector : this.constraintsRightVector,
      interpretationRanges: this.interpretationRangeVector,
      maximization: this.maximization,
      objectiveCoefficientVector: this.objectiveCoefficientVector
    };

    return requestObject;
  }
  
  private loadLPModel(){
    this.loadConstraintLeftSideMaxtrixFromView();
    this.loadConstraintsRightVectorFromView();
    this.loadObjectiveCoefficientVectorFromView();
    this.loadInterpretationRangeVectorFromView();

    this.loadConstraintConnectionsVectorFromView();
    this.loadOptimizationAimFromView();
  }

  private loadConstraintLeftSideMaxtrixFromView(): void {
    for(var i = 0; i < this.numberOfConstraints; ++i){
      for(var j = 0; j < this.numberOfDecisionVariables; ++j){
        // index in array starts from 0, but index of decision variables starts from 1!
        var foundValue = this.getConstraintCellValue(this.possibleSides[0], i + 1, j + 1);

        if(foundValue % 1 != 0){
          throw {message: `${this.NOT_INTEGER_ERROR_MSG} constraints (left side), variable: ${this.decisionVariableName}${j+1}, constraint: ${i+1}.`};
          // TODO: throw a valid exception like object with an error message
        }

        if(!this.constraintsLeftSideMatrix[i]){
          this.constraintsLeftSideMatrix[i] = [];
        }
        this.constraintsLeftSideMatrix[i][j] = foundValue;
      }
    }
  }

  private loadConstraintsRightVectorFromView(): void {
    for(var i = 0; i < this.numberOfConstraints; ++i){
      // index in array starts from 0, but index of decision variables starts from 1!
      var foundValue = this.getConstraintCellValue(this.possibleSides[1], i + 1, null);

      if(foundValue % 1 != 0){
        throw {message: `${this.NOT_INTEGER_ERROR_MSG} constraints (right side constant), constraint: ${i+1}.`};
        // TODO: throw a valid exception like object with an error message
      }

      this.constraintsRightVector[i] = foundValue;
    }
  }

  private loadConstraintConnectionsVectorFromView(): void {
    for(var i = 0; i < this.numberOfConstraints; ++i){
      // index in array starts from 0, but index of decision variables starts from 1!
      var foundValue = this.getConstraintSideConnectionValue(i + 1);
      this.constraintConnectionsVector[i] = foundValue;
    }
  }

  private loadInterpretationRangeVectorFromView(): void {
    for(var i = 0; i < this.numberOfDecisionVariables; ++i){
      // index in array starts from 0, but index of decision variables starts from 1!
      var foundValue = this.getInterpretationRangeLowerBoundForVariable(i + 1);

      if(foundValue != null && foundValue % 1 != 0){
        throw {message: `${this.NOT_INTEGER_ERROR_MSG} interpretation ranges, variable: ${this.decisionVariableName}${i+1}.`};
        // TODO: throw a valid exception like object with an error message
      }

      this.interpretationRangeVector[i] = foundValue;
    }
  }

  private loadOptimizationAimFromView(): void {
    var cell = document.getElementById(this.optimizationAimSelectId) as HTMLSelectElement;
    this.maximization = Boolean(cell.options[cell.selectedIndex].value);
  }

  private loadObjectiveCoefficientVectorFromView(): void {
    for(var i = 0; i < this.numberOfDecisionVariables; ++i){
      // index in array starts from 0, but index of decision variables starts from 1!
      var foundValue = this.getObjectiveVariableCoefficient(i + 1);

      if(foundValue % 1 != 0){
        throw {message: `${this.NOT_INTEGER_ERROR_MSG} objective function, variable: ${this.decisionVariableName}${i+1}.`};
        // TODO: throw a valid exception like object with an error message
      }

      this.objectiveCoefficientVector[i] = foundValue;
    }
  }

  private getConstraintCellValue(side: string, constraintNo: number, variableNo: number | null): number {
    var cellPreQuery = Array.from(document.getElementsByTagName("input"))
      .filter(htmlElement => htmlElement.classList.contains(side))
      .filter(htmlElement => htmlElement.hasAttribute(this.constraintAttributeName) && Number(htmlElement.getAttribute(this.constraintAttributeName)) == constraintNo);
    
    var cell;
    if(side === this.possibleSides[0]){
      cell = cellPreQuery.filter(htmlElement => htmlElement.hasAttribute(this.variableAttributeName) && Number(htmlElement.getAttribute(this.variableAttributeName)) == variableNo)[0];
    }
    else{
      cell = cellPreQuery[0];
    }
    return cell.value == "" ? 0 : Number(cell.value);
  }

  private getConstraintSideConnectionValue(constraintNo: number): number {
    var cell = Array.from(document.getElementsByTagName("select"))
      .filter(htmlElement => htmlElement.classList.contains(this.sideConnectionClassName))
      .filter(htmlElement => htmlElement.hasAttribute(this.constraintAttributeName) && Number(htmlElement.getAttribute(this.constraintAttributeName)) == constraintNo)[0];
    var cellValue = cell.options[cell.selectedIndex].value;
    // returns the numeric value of the SideConnection enum in the .NET API
    return cellValue == this.possibleSideConnections[0] ? 1 : (cellValue == this.possibleSideConnections[1] ? 2 : 3);
  }

  private getInterpretationRangeLowerBoundForVariable(variableIndex: number): number | null {
    var cell = Array.from(document.getElementsByTagName("input"))
      .filter(htmlElement => htmlElement.classList.contains(this.interpretationRangeClassName))
      .filter(htmlElement => htmlElement.hasAttribute(this.variableAttributeName) && Number(htmlElement.getAttribute(this.variableAttributeName)) == variableIndex)[0];
    return cell.value == "" ? null : Number(cell.value);
  }

  private getObjectiveVariableCoefficient(variableIndex: number): number {
    var cell = Array.from(document.getElementsByTagName("input"))
      .filter(htmlElement => htmlElement.classList.contains(this.objectiveFunctionClassName))
      .filter(htmlElement => htmlElement.hasAttribute(this.variableAttributeName) && Number(htmlElement.getAttribute(this.variableAttributeName)) == variableIndex)[0];
    return cell.value == "" ? 0 : Number(cell.value);
  }

  private hideElementById(id: string){
    var element = document.getElementById(id) as HTMLElement;
    element.setAttribute("hidden", "true");
  }

  private showElementById(id: string){
    var element = document.getElementById(id) as HTMLElement;
    element.removeAttribute("hidden");
  }
}